/*
 *  ARTrackedMarker.cs
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

/// <summary>
/// ARMarker objects represent an ARToolKit marker, even when ARToolKit is not
/// initialised.
/// To find markers from elsewhere in the Unity environment:
///   ARMarker[] markers = FindObjectsOfType<ARMarker>(); // (or FindObjectsOfType(typeof(ARMarker)) as ARMarker[]);
/// </summary>
/// 
[ExecuteInEditMode]
public class ARTrackedMarker : MonoBehaviour {
	public enum ARWMarkerOption : int {
		ARW_MARKER_OPTION_FILTERED                        = 1,
		ARW_MARKER_OPTION_FILTER_SAMPLE_RATE              = 2,
		ARW_MARKER_OPTION_FILTER_CUTOFF_FREQ              = 3,
		ARW_MARKER_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION = 4,
		ARW_MARKER_OPTION_SQUARE_CONFIDENCE               = 5,
		ARW_MARKER_OPTION_SQUARE_CONFIDENCE_CUTOFF        = 6,
		ARW_MARKER_OPTION_NFT_SCALE                       = 7
	}

    public readonly static Dictionary<MarkerType, string> MarkerTypeNames = new Dictionary<MarkerType, string> {
		{MarkerType.Square,        "Single AR Pattern"},
		{MarkerType.SquareBarcode, "Single AR Barcode"},
    	{MarkerType.Multimarker,   "Multimarker AR Configuration"},
		{MarkerType.NFT,           "NFT Dataset"}
    };

    private const string LOG_TAG = "ARTrackedMarker: ";

	private const int    NO_ID   = -1;

	private const string NFT_FORMAT            = "ARToolKit/NFT/{0}";
	private const string MULTI_FORMAT		   = "ARToolKit";
	
	private const string SINGLE_BUFFER_CONFIG  = "single_buffer;{0};buffer={1}";
	private const string SINGLE_BARCODE_CONFIG = "single_barcode;{0};{1}";
	private const string MULTI_CONFIG          = "multi;{0}";
	private const string NFT_CONFIG            = "nft;{0}";
	
	public const float ARTOOLKIT_TO_UNITY    = 1000.0f;
	public const float UNITY_TO_ARTOOLKIT    = 1.0f / ARTOOLKIT_TO_UNITY;
	
	private const string LOAD_FAILURE         = LOG_TAG + "Failed to load {0}. Quitting.";

	#region Editor
	// UnityEditor doesn't serialize properties.
	// In order to keep track of what we're using, we serialize their properties here,
	// rather than using some ugly ID association with EditorPrefs.
	// These are not #if'd out because that would change the serialization layout of the class.
	// TODO: Remove this by dynamic lookup of these values based on actually used
	// serialized information.
	public        int    EditorMarkerIndex    = 0;
	public        string EditorMarkerName     = string.Empty;
	#endregion

	// Current Unique Identifier (UID) assigned to this marker.
	// UID is not serialized because its value is only meaningful during a specific run.
    public int UID {
		get {
			return uid;
		}
	}

    // Public members get serialized
	public MarkerType MarkerType {
		get {
			return markerType;
		}
		set {
			if (value != markerType) {
				Unload();
				markerType = value;
				Load();
			}
		}
	}

	public string PatternContents {
		get {
			return patternContents;
		}
		set {
			if (value != patternContents) {
				Unload();
				patternContents = value;
				Load();
			}
		}
	}

	public float PatternWidth {
		get {
			return patternWidth;
		}
		set {
			if (value != patternWidth) {
				Unload();
				patternWidth = value;
				Load();
			}
		}
	}
	
	// Barcode markers have a user-selected ID.
	public int BarcodeID {
		get {
			return barcodeID;
		}
		set {
			if (value != barcodeID) {
				Unload();
				barcodeID = value;
				Load();
			}
		}
	}
	
    // If the marker is multi, it just has a config filename
	public string MultiConfigFile {
		get {
			return multiConfigFile;
		}
		set {
			if (value != multiConfigFile) {
				Unload();
				multiConfigFile = value;
				Load();
			}
		}
	}
	
	// NFT markers have a dataset pathname (less the extension).
	// Also, we need a list of the file extensions that make up an NFT dataset.
	public string NFTDataName {
		get {
			return nftDataName;
		}
		set {
			if (value != nftDataName) {
				Unload();
				nftDataName = value;
				Load();
			}
		}
	}

	public float NFTWidth {
		get {
			return nftWidth;
		}
	}

	public float NFTHeight {
		get {
			return nftHeight;
		}
	}

	public Matrix4x4 TransformationMatrix {
		get {
			return transformationMatrix;
		}
	}
	
	public bool Visible {
		get {
			return visible;
		}
	}
	
	public ARPattern[] Patterns {
		get {
			return patterns;
		}
	}
	
	public bool Filtered {
		get {
			return currentFiltered;
		}
		set {
			if (value == currentFiltered) {
				return;
			}
			currentFiltered = value;
			lock (loadLock) {
				if (UID == NO_ID) {
					return;
				}
				PluginFunctions.arwSetMarkerOptionBool(UID, (int)ARWMarkerOption.ARW_MARKER_OPTION_FILTERED, value);
			}
		}
	}
	
	public float FilterSampleRate {
		get {
			return currentFilterSampleRate;
		}
		set {
			if (value == currentFilterSampleRate) {
				return;
			}
			lock (loadLock) {
				if (UID == NO_ID) {
					return;
				}
				PluginFunctions.arwSetMarkerOptionFloat(UID, (int)ARWMarkerOption.ARW_MARKER_OPTION_FILTER_SAMPLE_RATE, value);
			}
		}
	}
	
	public float FilterCutoffFreq {
		get {
			return currentFilterCutoffFreq;
		}
		set {
			if (value == currentFilterCutoffFreq) {
				return;
			}
			currentFilterCutoffFreq = value;
			lock (loadLock) {
				if (UID == NO_ID) {
					return;
				}
				PluginFunctions.arwSetMarkerOptionFloat(UID, (int)ARWMarkerOption.ARW_MARKER_OPTION_FILTER_CUTOFF_FREQ, value);
			}
		}
	}
	
	public bool UseContPoseEstimation {
		get {
			return currentUseContPoseEstimation;
		}
		set {
			currentUseContPoseEstimation = value;
			if (MarkerType != MarkerType.Square && MarkerType != MarkerType.SquareBarcode) {
				return;
			}
			lock (loadLock) {
				if (UID == NO_ID) {
					return;
				}
				PluginFunctions.arwSetMarkerOptionBool(UID, (int)ARWMarkerOption.ARW_MARKER_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, value);
			}
		}
	}
	
	public float NFTScale {
		get {
			return currentNFTScale;
		}
		set {
			if (value == currentNFTScale) {
				return;
			}
			currentNFTScale = value;
			if (MarkerType != MarkerType.NFT) {
				return;
			}
			lock (loadLock) {
				if (UID == NO_ID) {
					return;
				}
				PluginFunctions.arwSetMarkerOptionFloat(UID, (int)ARWMarkerOption.ARW_MARKER_OPTION_NFT_SCALE, value);
			}
		}
	}

	public List<IAREventReciever> eventRecievers = new List<IAREventReciever>();

	#if !UNITY_METRO
	private readonly string[] NFTDataExts = {".iset", ".fset", ".fset3"};
	#endif
	
	
	[NonSerialized] private int         uid                  = NO_ID;
	[NonSerialized] private float       nftWidth             = 1.0f;               // Once marker is loaded, this holds the width of the marker in Unity units.
	[NonSerialized] private float       nftHeight            = 1.0f;               // Once marker is loaded, this holds the height of the marker in Unity units.
	[NonSerialized] private ARPattern[] patterns             = null;               // Single markers have a single pattern, multi markers have one or more, NFT have none.
	[NonSerialized] private bool        visible              = false;              // Marker is visible or not.
	[NonSerialized] private Matrix4x4   transformationMatrix = Matrix4x4.identity; // Full transformation matrix as a Unity matrix.

	// Private fields with accessors.
	// Marker configuration options.
	// Normally set through Inspector Editor script.
	[SerializeField] private MarkerType markerType                   = MarkerType.Square;
	[SerializeField] private bool       currentFiltered              = false;
	[SerializeField] private float      currentFilterSampleRate      = 30.0f;
	[SerializeField] private float      currentFilterCutoffFreq      = 15.0f;
	// NFT Marker Only
	[SerializeField] private string     nftDataName                  = string.Empty;
	[SerializeField] private float      currentNFTScale              = 1.0f;          // Scale factor applied to marker size.
	// Single Marker Only
	[SerializeField] private float      patternWidth                 = 0.08f;         // Width of pattern in meters.
	[SerializeField] private bool       currentUseContPoseEstimation = false;         // Whether continuous pose estimation should be used.
	// Single Non-Barcode Marker Only
	[SerializeField] private string     patternContents              = string.Empty;
	// Single Barcode Marker Only
	[SerializeField] private int        barcodeID                    = 0;
	// Multimarker Only
	[SerializeField] private string     multiConfigFile              = string.Empty;
	
	private object loadLock = new object();

	// Load the underlying ARToolKit marker structure(s) and set the UID.
	public void Load() {
		lock (loadLock) {
			if (UID != NO_ID || !PluginFunctions.inited) {
				return;
			}
			bool running = ARController.Instance.Running;
			if (running) {
				ARController.Log(LOG_TAG +"Attempting to add marker while AR is running.");
				ARController.Instance.StopAR();
			}
			// Work out the configuration string to pass to ARToolKit.
			string assetDirectory = Application.streamingAssetsPath;
			string configuration  = string.Empty;
			
			switch (MarkerType) {
				case MarkerType.Square:
					// Multiply width by 1000 to convert from metres to ARToolKit's millimetres.
					configuration = string.Format(SINGLE_BUFFER_CONFIG, PatternWidth * ARTOOLKIT_TO_UNITY, PatternContents);
					break;
				case MarkerType.SquareBarcode:
					// Multiply width by 1000 to convert from metres to ARToolKit's millimetres.
					configuration = string.Format(SINGLE_BARCODE_CONFIG, BarcodeID, PatternWidth * ARTOOLKIT_TO_UNITY);
					break;
				case MarkerType.Multimarker:
					if (string.IsNullOrEmpty(MultiConfigFile)) {
						ARController.Log(string.Format(LOAD_FAILURE, "multimarker due to no MultiConfigFile"));
						return;
					}
					string path = Path.Combine(MULTI_FORMAT, MultiConfigFile);
					ARUtilityFunctions.GetFileFromStreamingAssets(path, out assetDirectory);
					if (!string.IsNullOrEmpty(assetDirectory)) {
						configuration = string.Format(MULTI_CONFIG, assetDirectory);
					}
					break;			
				case MarkerType.NFT:
					if (string.IsNullOrEmpty(NFTDataName)) {
						ARController.Log(string.Format(LOAD_FAILURE, "NFT marker due to no NFTDataName"));
						return;
					}
				string relative = string.Format(NFT_FORMAT, NFTDataName);
					foreach (string ext in NFTDataExts) {
						assetDirectory = string.Empty;
					string temp = relative + ext;
						if (!ARUtilityFunctions.GetFileFromStreamingAssets(temp, out assetDirectory)) {
							ARController.Log(string.Format(LOAD_FAILURE, relative));
							return;
					}
					}
					if (!string.IsNullOrEmpty(assetDirectory)) {
						configuration = string.Format(NFT_CONFIG, assetDirectory.Split('.')[0]);
					}
					break;
				default:
					// Unknown marker type?
					ARController.Log(string.Format(LOAD_FAILURE, "due to unknown marker"));
					return;
			}
			
			// If a valid config. could be assembled, get ARToolKit to process it, and assign the resulting ARMarker UID.
			if (string.IsNullOrEmpty(configuration)) {
				ARController.Log(LOG_TAG + "Config is null or empty.");
				return;
			}
			
			uid = PluginFunctions.arwAddMarker(configuration);
			if (UID == NO_ID) {
				ARController.Log(LOG_TAG + "Error loading marker.");
				return;
			}
			
			// Marker loaded. Do any additional configuration.
			if (MarkerType == MarkerType.Square || MarkerType == MarkerType.SquareBarcode) {
				UseContPoseEstimation = currentUseContPoseEstimation;
			}
			
			Filtered         = currentFiltered;
			FilterSampleRate = currentFilterSampleRate;
			FilterCutoffFreq = currentFilterCutoffFreq;
			
			// Retrieve any required information from the configured ARToolKit ARMarker.
			if (MarkerType == MarkerType.NFT) {
				NFTScale = currentNFTScale;
				int imageSizeX, imageSizeY;
				PluginFunctions.arwGetMarkerPatternConfig(UID, 0, null, out nftWidth, out nftHeight, out imageSizeX, out imageSizeY);
				nftWidth  *= UNITY_TO_ARTOOLKIT;
				nftHeight *= UNITY_TO_ARTOOLKIT;
			} else {
				// Create array of patterns. A single marker will have array length 1.
				int numPatterns = PluginFunctions.arwGetMarkerPatternCount(UID);
				if (numPatterns > 0) {
					patterns = new ARPattern[numPatterns];
					for (int i = 0; i < numPatterns; ++i) {
						patterns[i] = new ARPattern(UID, i);
					}
				}
			}
			if (running && !ARController.Instance.Running) {
				ARController.Instance.StartAR();
			}
		}
	}
	
	// Unload any underlying ARToolKit structures, and clear the UID.
	public void Unload() {
		lock (loadLock) {
			if (UID == NO_ID) {
				return;
			}
			if (PluginFunctions.inited) {
				PluginFunctions.arwRemoveMarker(UID);
			} else {
				ARController.Log(LOG_TAG + "Unload: PluginFunctions not inited!");
			}
			uid = NO_ID;
			patterns = null; // Delete the patterns too.
		}
	}

	private void Start() {
		if (Application.isPlaying) {
			for (int i = 0; i < transform.childCount; ++i) {
				this.transform.GetChild(i).gameObject.SetActive(false);
			}
		}
	}
	
	private void OnEnable() {
		Load();
	}
	
	private void OnDisable() {
		Unload();
	}

	// 1 - Query for visibility.
	// 2 - Determine if visibility state is new.
	// 3 - If visible, calculate marker pose.
	// 4 - If visible, set marker pose.
	// 5 - If visibility state is new, notify event recievers via "OnMarkerFound" or "OnMarkerLost".
	// 6 - If visibility state is new, set appropriate active state for marker children.
	// 7 - If visible, notify event recievers that the marker's pose has been updated via "OnMarkerTracked".
	private void LateUpdate() {
		if (!Application.isPlaying) {
			return;
		}

		float[] matrixRawArray = new float[16];
		lock(loadLock) {
			if (UID == NO_ID || !PluginFunctions.inited) {
				visible = false;
				return;
			}
			
			Vector3 storedScale  = transform.localScale;
			transform.localScale = Vector3.one;
			
			// 1 - Query for visibility.
			bool nowVisible = PluginFunctions.arwQueryMarkerTransformation(UID, matrixRawArray);
			
			// 2 - Determine if visibility state is new.
			bool notify = (nowVisible && !visible) || (!nowVisible && visible);
			visible = nowVisible;
			
			// 3 - If visible, calculate marker pose.
			if (visible) {
				// Scale the position from ARToolKit units (mm) into Unity units (m).
				matrixRawArray[12] *= UNITY_TO_ARTOOLKIT;
				matrixRawArray[13] *= UNITY_TO_ARTOOLKIT;
				matrixRawArray[14] *= UNITY_TO_ARTOOLKIT;
				
				Matrix4x4 matrixRaw = ARUtilityFunctions.MatrixFromFloatArray(matrixRawArray);
				// ARToolKit uses right-hand coordinate system where the marker lies in x-y plane with right in direction of +x,
				// up in direction of +y, and forward (towards viewer) in direction of +z.
				// Need to convert to Unity's left-hand coordinate system where marker lies in x-y plane with right in direction of +x,
				// up in direction of +y, and forward (towards viewer) in direction of -z.
				transformationMatrix = ARUtilityFunctions.LHMatrixFromRHMatrix(matrixRaw);
				
				// 4 - If visible, set marker pose.
				Matrix4x4 pose;
//				AROrigin origin = gameObject.GetComponentInParent<AROrigin>();
//				if (null == origin) {
//					pose = transformationMatrix;
//				} else if (this == origin.GetBaseMarker()) {
//					// If there is no origin, or this marker is the base, no need to take base inverse etc.
//					pose = origin.transform.localToWorldMatrix;
//				} else {
//					// If this marker is not the base, need to take base inverse etc.
//					pose = (origin.transform.localToWorldMatrix * origin.GetBaseMarker().TransformationMatrix.inverse * transformationMatrix);
				pose = ARStaticCamera.Instance.transform.localToWorldMatrix * transformationMatrix;
//				}
				transform.position   = ARUtilityFunctions.PositionFromMatrix(pose);
				transform.rotation   = ARUtilityFunctions.RotationFromMatrix(pose);
				transform.localScale = storedScale;
			}
			
			// 5 - If visibility state is new, notify event recievers via "OnMarkerFound" or "OnMarkerLost".
			if (notify) {
				if (null != eventRecievers && eventRecievers.Count > 0) {
					if (visible) {
						eventRecievers.ForEach(x => x.OnMarkerFound(this));
					} else {
						eventRecievers.ForEach(x => x.OnMarkerLost(this));
					}
				}
				// 6 - If visibility state is new, set appropriate active state for marker children.
				for (int i = 0; i < transform.childCount; ++i) {
					transform.GetChild(i).gameObject.SetActive(visible);
				}
			}
			
			// 7 - If visible, notify event recievers that the marker's pose has been updated via "OnMarkerTracked".
			if (null != eventRecievers&& eventRecievers.Count > 0) {
				eventRecievers.ForEach(x => x.OnMarkerTracked(this));
			}
		}
	}
}
