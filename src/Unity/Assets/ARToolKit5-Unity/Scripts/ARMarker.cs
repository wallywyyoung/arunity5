/*
 *  ARMarker.cs
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
 *  Copyright 2010-2015 ARToolworks, Inc.
 *
 *  Author(s): Philip Lamb, Julian Looser, Wally Young
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

public enum MarkerType
{
    Square,      		// A standard ARToolKit template (pattern) marker
    SquareBarcode,      // A standard ARToolKit matrix (barcode) marker.
    Multimarker,        // Multiple markers treated as a single target
	NFT
}

public enum ARWMarkerOption : int {
        ARW_MARKER_OPTION_FILTERED = 1,
        ARW_MARKER_OPTION_FILTER_SAMPLE_RATE = 2,
        ARW_MARKER_OPTION_FILTER_CUTOFF_FREQ = 3,
        ARW_MARKER_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION = 4,
		ARW_MARKER_OPTION_SQUARE_CONFIDENCE = 5,
		ARW_MARKER_OPTION_SQUARE_CONFIDENCE_CUTOFF = 6,
		ARW_MARKER_OPTION_NFT_SCALE = 7
}

/// <summary>
/// ARMarker objects represent an ARToolKit marker, even when ARToolKit is not
/// initialised.
/// To find markers from elsewhere in the Unity environment:
///   ARMarker[] markers = FindObjectsOfType<ARMarker>(); // (or FindObjectsOfType(typeof(ARMarker)) as ARMarker[]);
/// 
/// </summary>
/// 
[ExecuteInEditMode]
public class ARMarker : ARTrackedMarker {
	public string Tag = "";

	// We use Update() here, but be aware that unless ARController has been configured to
	// execute first (Unity Editor->Edit->Project Settings->Script Execution Order) then
	// state produced by this update may lag by one frame.
    protected override void LateUpdate()
    {
		if (UID == NO_ID || !PluginFunctions.inited) {
            visible = false;
            return;
		}

		float[] matrixRawArray = new float[16];

		// Query visibility if we are running in the Player.
        if (Application.isPlaying) {

			visible = PluginFunctions.arwQueryMarkerTransformation(UID, matrixRawArray);
			
            if (visible) {
				matrixRawArray[12] *= 0.001f; // Scale the position from ARToolKit units (mm) into Unity units (m).
				matrixRawArray[13] *= 0.001f;
				matrixRawArray[14] *= 0.001f;

				Matrix4x4 matrixRaw = ARUtilityFunctions.MatrixFromFloatArray(matrixRawArray);
				//ARController.Log("arwQueryMarkerTransformation(" + UID + ") got matrix: [" + Environment.NewLine + matrixRaw.ToString("F3").Trim() + "]");

				// ARToolKit uses right-hand coordinate system where the marker lies in x-y plane with right in direction of +x,
				// up in direction of +y, and forward (towards viewer) in direction of +z.
				// Need to convert to Unity's left-hand coordinate system where marker lies in x-y plane with right in direction of +x,
				// up in direction of +y, and forward (towards viewer) in direction of -z.
				transformationMatrix = ARUtilityFunctions.LHMatrixFromRHMatrix(matrixRaw);
			}
		}
    }
}
