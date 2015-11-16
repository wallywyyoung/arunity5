/*
 *  ARStaticCamera.cs
 *  ARToolKit for Unity
 *
 *  This file is part of ARToolKit for Unity.
 *
 *  ARToolKit for Unity is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  ARToolKit for Unity is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with ARToolKit for Unity.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  As a special exception, the copyright holders of this library give you
 *  permission to link this library with independent modules to produce an
 *  executable, regardless of the license terms of these independent modules, and to
 *  copy and distribute the resulting executable under terms of your choice,
 *  provided that you also meet, for each linked independent module, the terms and
 *  conditions of the license of that module. An independent module is a module
 *  which is neither derived from nor based on this library. If you modify this
 *  library, you may extend this exception to your version of the library, but you
 *  are not obligated to do so. If you do not wish to do so, delete this exception
 *  statement from your version.
 *
 *  Copyright 2015 Daqri, LLC.
 *
 *  Author(s): Wally Young
 *
 */

using UnityEngine;
using System.Collections;
using System.Linq;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class ARStaticCamera : MonoBehaviour {
	private const string LOG_TAG           = "ARStaticCamera: ";
	private const string OPTICAL_LOG       = LOG_TAG + "Optical parameters: fovy={0}, aspect={1}, camera position (m)={{2}, {3}, {4}}";
	private const string RIGHT_EYE_NAME    = "ARCamera Right Eye";
	private const float  NO_LATERAL_OFFSET = 0.0f;

	private static ARStaticCamera instance = null;
	public  static ARStaticCamera Instance {
		get {
			if (null == instance) {
				instance = GameObject.FindObjectOfType<ARStaticCamera>();
			}
			return instance;
		}
	}

	#region Editor
	// UnityEditor doesn't serialize properties.
	// In order to keep track of what we're using, we serialize their properties here,
	// rather than using some ugly ID association with EditorPrefs.
	// These are not #if'd out because that would change the serialization layout of the class.
	// TODO: Remove this by dynamic lookup of these values based on actually used
	// serialized information.
	public  int       EditorOpticalIndexL    = 0;
	public  string    EditorOpticalNameL     = null;
	public  int       EditorOpticalIndexR    = 0;
	public  string    EditorOpticalNameR     = null;
	#endregion

	public  bool      Stereo				  = false;
	public  bool      Optical                 = false;

	public  byte[]    OpticalParametersL      = null;
	public  byte[]    OpticalParametersR      = null;
	// Average of male/female IPD from https://en.wikipedia.org/wiki/Interpupillary_distance
	public  float     OpticalEyeLateralOffset = 63.5f;

	
	private Matrix4x4 opticalViewMatrix       = Matrix4x4.identity;
	public  Matrix4x4 OpticalViewMatrix {
		get {
			return opticalViewMatrix;
		}
	}
	
	private Camera leftCamera = null;
	private Camera LeftCamera {
		get {
			if (null == leftCamera) {
				leftCamera = GetComponent<Camera>();
			}
			return leftCamera;
		}
	}
	
	private Camera rightCamera = null;
	private Camera RightCamera {
		get {
			if (null == rightCamera) {
				GameObject rightEye = new GameObject(RIGHT_EYE_NAME);
				rightEye.transform.parent        = gameObject.transform;
				rightEye.transform.localPosition = Vector3.zero;
				rightEye.transform.localRotation = Quaternion.identity;
				rightEye.transform.localScale    = Vector3.zero;
				rightCamera = rightEye.AddComponent<Camera>();
			}
			return rightCamera;
		}
	}
	
	private bool useLateralOffset {
		get {
			return Optical && !OpticalParametersL.SequenceEqual(OpticalParametersR);
		}
	}

	private void Awake() {
		if (null == instance) {
			instance = this;
		} else {
			Debug.LogError("ERROR: MORE THAN ONE ARSTATICCAMERA IN SCENE!");
		}
	}

	public void ConfigureViewports(Rect pixelRectL, Rect pixelRectR) {
		LeftCamera.pixelRect = pixelRectL;
		if (Stereo) {
			RightCamera.pixelRect = pixelRectR;
		}
	}

	public bool SetupCamera(Matrix4x4 projectionMatrixL, Matrix4x4 projectionMatrixR, ref bool opticalOut) {
		opticalOut = Optical;

		bool success = SetupCamera(projectionMatrixL, LeftCamera, Optical ? OpticalParametersL : null, NO_LATERAL_OFFSET);
		if (Stereo) {
			success = success && SetupCamera(projectionMatrixR, RightCamera, Optical ? OpticalParametersR : null, useLateralOffset ? OpticalEyeLateralOffset : NO_LATERAL_OFFSET);
		}
		return success;
	}

	private bool SetupCamera(Matrix4x4 projectionMatrix, Camera referencedCamera, byte[] opticalParameters, float lateralOffset) {
		// A perspective projection matrix from the tracker
		referencedCamera.orthographic = false;
		
		if (null == opticalParameters) {
			referencedCamera.projectionMatrix = projectionMatrix;
		} else {
			float fovy ;
			float aspect;
			float[] m = new float[16];
			float[] p = new float[16];
			bool opticalSetupOK = PluginFunctions.arwLoadOpticalParams(null, opticalParameters, opticalParameters.Length, out fovy, out aspect, m, p);
			if (!opticalSetupOK) {
				ARController.Log(LOG_TAG + "Error loading optical parameters.");
				return false;
			}
			m[12] *= ARTrackedMarker.UNITY_TO_ARTOOLKIT;
			m[13] *= ARTrackedMarker.UNITY_TO_ARTOOLKIT;
			m[14] *= ARTrackedMarker.UNITY_TO_ARTOOLKIT;
			ARController.Log(string.Format(OPTICAL_LOG, fovy, aspect, m[12].ToString("F3"), m[13].ToString("F3"), m[14].ToString("F3")));
			
			referencedCamera.projectionMatrix = ARUtilityFunctions.MatrixFromFloatArray(p);
			
			opticalViewMatrix = ARUtilityFunctions.MatrixFromFloatArray(m);
			if (lateralOffset != NO_LATERAL_OFFSET) {
				opticalViewMatrix = Matrix4x4.TRS(new Vector3(-lateralOffset, 0.0f, 0.0f), Quaternion.identity, Vector3.one) * opticalViewMatrix; 
			}
			// Convert to left-hand matrix.
			opticalViewMatrix = ARUtilityFunctions.LHMatrixFromRHMatrix(opticalViewMatrix);
		}
		
		// Don't clear anything or else we interfere with other foreground cameras
		referencedCamera.clearFlags = CameraClearFlags.Nothing;

		// Renders after the clear and background cameras
		referencedCamera.depth = 2;
		
		// Ensure background camera isn't rendered in ARCamera.
		referencedCamera.cullingMask &= ARController.Instance.BackgroundLayer0;
		if (ARController.Instance.VideoIsStereo) {
			referencedCamera.cullingMask &= ARController.Instance.BackgroundLayer1;
		}
		return true;
	}

}
