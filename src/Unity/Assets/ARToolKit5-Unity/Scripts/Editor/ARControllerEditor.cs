/*
 *  ARControllerEditor.cs
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
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ARController))]
public class ARControllerEditor : Editor {

	private bool showVideoConfiguration    = false;
	private bool showVideoOptions          = false;
    private bool showThresholdOptions      = false;
    private bool showSquareTrackingOptions = false;
//	private bool showNFTTrackingOptions    = false;
	private bool showApplicationOptions    = false;

    public override void OnInspectorGUI()
    {

        ARController arcontroller = (ARController)target;
        if (arcontroller == null) {
			return;
		}

        EditorGUILayout.LabelField("Version", "ARToolKit " + arcontroller.Version);

        EditorGUILayout.Separator();
		if (!arcontroller.VideoIsStereo) {
			if (cameras == null || cameras[0] == "") {
				cameras = ARToolKitAssetManager.GetCameras();
			}
			cameraIndex = EditorGUILayout.Popup("Camera Parameter", cameraIndex, cameras);
			if (string.Compare(cameras[cameraIndex], arcontroller.videoCParamName0, StringComparison.Ordinal) != 0) {
				arcontroller.videoCParamName0 = cameras[cameraIndex];
			}
		} else {
			cameraIndex = EditorGUILayout.Popup("Camera Parameter (L)", cameraIndex, cameras);
			if (string.Compare(cameras[cameraIndex], arcontroller.videoCParamName0, StringComparison.Ordinal) != 0) {
				arcontroller.videoCParamName0 = cameras[cameraIndex];
			}
			cameraIndexR = EditorGUILayout.Popup("Camera Parameter (R)", cameraIndexR, cameras);
			if (string.Compare(cameras[cameraIndexR], arcontroller.videoCParamName1, StringComparison.Ordinal) != 0) {
				arcontroller.videoCParamName1 = cameras[cameraIndexR];
			}
		}

		showVideoConfiguration = EditorGUILayout.Foldout(showVideoConfiguration, "Video Configuration");
		if (showVideoConfiguration)
		{
			if (!arcontroller.VideoIsStereo) {
				arcontroller.videoConfigurationAndroid0      = EditorGUILayout.TextField("Android",           arcontroller.videoConfigurationAndroid0);
				arcontroller.videoConfigurationiOS0          = EditorGUILayout.TextField("iOS",               arcontroller.videoConfigurationiOS0);
				arcontroller.videoConfigurationLinux0        = EditorGUILayout.TextField("Linux",             arcontroller.videoConfigurationLinux0);
				arcontroller.videoConfigurationMacOSX0       = EditorGUILayout.TextField("Mac OS X",          arcontroller.videoConfigurationMacOSX0);
				arcontroller.videoConfigurationWindows0      = EditorGUILayout.TextField("Windows",           arcontroller.videoConfigurationWindows0);
				arcontroller.videoConfigurationWindowsStore0 = EditorGUILayout.TextField("Windows Store",     arcontroller.videoConfigurationWindowsStore0);
			} else {
				arcontroller.videoConfigurationAndroid0      = EditorGUILayout.TextField("Android (L)",       arcontroller.videoConfigurationAndroid0);
				arcontroller.videoConfigurationAndroid1      = EditorGUILayout.TextField("Android (R)",       arcontroller.videoConfigurationAndroid1);
				arcontroller.videoConfigurationiOS0          = EditorGUILayout.TextField("iOS (L)",           arcontroller.videoConfigurationiOS0);
				arcontroller.videoConfigurationiOS1          = EditorGUILayout.TextField("iOS (R)",           arcontroller.videoConfigurationiOS1);
				arcontroller.videoConfigurationLinux0        = EditorGUILayout.TextField("Linux (L)",         arcontroller.videoConfigurationLinux0);
				arcontroller.videoConfigurationLinux1        = EditorGUILayout.TextField("Linux (R)",         arcontroller.videoConfigurationLinux1);
				arcontroller.videoConfigurationMacOSX0       = EditorGUILayout.TextField("OS X (L)",          arcontroller.videoConfigurationMacOSX0);
				arcontroller.videoConfigurationMacOSX1       = EditorGUILayout.TextField("OS X (R)",          arcontroller.videoConfigurationMacOSX1);
				arcontroller.videoConfigurationWindows0      = EditorGUILayout.TextField("Windows (L)",       arcontroller.videoConfigurationWindows0);
				arcontroller.videoConfigurationWindows1      = EditorGUILayout.TextField("Windows (R)",       arcontroller.videoConfigurationWindows1);
				arcontroller.videoConfigurationWindowsStore0 = EditorGUILayout.TextField("Windows Store (L)", arcontroller.videoConfigurationWindowsStore0);
				arcontroller.videoConfigurationWindowsStore1 = EditorGUILayout.TextField("Windows Store (R)", arcontroller.videoConfigurationWindowsStore1);
			}
		}

		showVideoOptions = EditorGUILayout.Foldout(showVideoOptions, "Video Background");
		if (showVideoOptions) {
			arcontroller.BackgroundLayer0 = EditorGUILayout.LayerField("Background Layer", arcontroller.BackgroundLayer0);

			arcontroller.UseNativeGLTexturingIfAvailable = EditorGUILayout.Toggle("Native GL Texturing", arcontroller.UseNativeGLTexturingIfAvailable);
			if (arcontroller.UseNativeGLTexturingIfAvailable) {
				EditorGUILayout.HelpBox("Warning: Native GL Texturing is not availible on all platforms!", MessageType.Warning);
				EditorGUI.BeginDisabledGroup(true);
				arcontroller.AllowNonRGBVideo = EditorGUILayout.Toggle("Process Non-RGB Video", false);
				EditorGUI.EndDisabledGroup();
			} else {
				arcontroller.AllowNonRGBVideo = EditorGUILayout.Toggle("Process Non-RGB Video", arcontroller.AllowNonRGBVideo);
			}


			ContentMode currentContentMode = arcontroller.ContentMode;
			ContentMode newContentMode = (ContentMode)EditorGUILayout.EnumPopup("Content Mode", currentContentMode);
			if (newContentMode != currentContentMode) {
				arcontroller.ContentMode = newContentMode;
			}
			arcontroller.ContentRotate90 = EditorGUILayout.Toggle("Rotate 90° Clockwise", arcontroller.ContentRotate90);
			arcontroller.ContentFlipV    = EditorGUILayout.Toggle("Flip Vertically",      arcontroller.ContentFlipV);
			arcontroller.ContentFlipH    = EditorGUILayout.Toggle("Flip Horizontally",    arcontroller.ContentFlipH);
		}

        showThresholdOptions = EditorGUILayout.Foldout(showThresholdOptions, "Threshold Options");
        if (showThresholdOptions)
        {
            // Threshold mode selection
            ARController.ARToolKitThresholdMode currentThreshMode = arcontroller.VideoThresholdMode;
            ARController.ARToolKitThresholdMode newThreshMode = (ARController.ARToolKitThresholdMode)EditorGUILayout.EnumPopup("Mode", currentThreshMode);
            if (newThreshMode != currentThreshMode) {
                arcontroller.VideoThresholdMode = newThreshMode;
            }
			EditorGUILayout.HelpBox(ARController.ThresholdModeDescriptions[newThreshMode], MessageType.Info);

            // Show threshold slider only in manual or bracketing modes.
			if (newThreshMode == ARController.ARToolKitThresholdMode.Manual || newThreshMode == ARController.ARToolKitThresholdMode.Bracketing) {
                int currentThreshold = arcontroller.VideoThreshold;
                int newThreshold = EditorGUILayout.IntSlider("Threshold", currentThreshold, 0, 255);
                if (newThreshold != currentThreshold) {
                    arcontroller.VideoThreshold = newThreshold;
                }
            }
        }

		showSquareTrackingOptions = EditorGUILayout.Foldout(showSquareTrackingOptions, "Square Tracking Options");
		if (showSquareTrackingOptions) {
			arcontroller.TemplateSize = EditorGUILayout.IntSlider("Template Size (bits)", arcontroller.TemplateSize, 16, 64);

			int currentTemplateCountMax = arcontroller.TemplateCountMax;
			int newTemplateCountMax = EditorGUILayout.IntField("Maximum Template Count", currentTemplateCountMax);
			if (newTemplateCountMax != currentTemplateCountMax && newTemplateCountMax > 0) {
				arcontroller.TemplateCountMax = newTemplateCountMax;
			}

			arcontroller.BorderSize           = UnityEngine.Mathf.Clamp(EditorGUILayout.FloatField("Border Size (%)", arcontroller.BorderSize), 0.0f, 0.5f);

			arcontroller.LabelingMode         = (ARController.ARToolKitLabelingMode)EditorGUILayout.EnumPopup("Marker Border Color", arcontroller.LabelingMode);

			arcontroller.PatternDetectionMode = (ARController.ARToolKitPatternDetectionMode)EditorGUILayout.EnumPopup("Pattern Detection Mode", arcontroller.PatternDetectionMode);
 
			// Matrix code type selection (only when in one of the matrix modes).
			if (   arcontroller.PatternDetectionMode == ARController.ARToolKitPatternDetectionMode.AR_MATRIX_CODE_DETECTION
			    || arcontroller.PatternDetectionMode == ARController.ARToolKitPatternDetectionMode.AR_TEMPLATE_MATCHING_COLOR_AND_MATRIX
			    || arcontroller.PatternDetectionMode == ARController.ARToolKitPatternDetectionMode.AR_TEMPLATE_MATCHING_MONO_AND_MATRIX) {
				 arcontroller.MatrixCodeType = (ARController.ARToolKitMatrixCodeType)EditorGUILayout.EnumPopup("Matrix Code Type", arcontroller.MatrixCodeType);
			}

			arcontroller.ImageProcMode = (ARController.ARToolKitImageProcMode)EditorGUILayout.EnumPopup("Image Processing Mode", arcontroller.ImageProcMode); 
		}

		// Removed until working.
//		EditorGUILayout.Separator();
//		
//		showNFTTrackingOptions = EditorGUILayout.Foldout(showNFTTrackingOptions, "NFT Tracking Options");
//		if (showNFTTrackingOptions) {
//			arcontroller.NFTMultiMode = EditorGUILayout.Toggle("Multi-page mode", arcontroller.NFTMultiMode);
//		}

		showApplicationOptions = EditorGUILayout.Foldout(showApplicationOptions, "Additional Options");
		if (showApplicationOptions) {
			arcontroller.AutoStartAR = EditorGUILayout.Toggle("Auto-Start AR.", arcontroller.AutoStartAR);
			if (arcontroller.AutoStartAR) {
				EditorGUILayout.HelpBox("ARController.StartAR() will be called during MonoBehavior.Start().", MessageType.Info);
			} else {
				EditorGUILayout.HelpBox("ARController.StartAR() will not be called during MonoBehavior.Start(); you must call it yourself.", MessageType.Warning);
			}

			arcontroller.QuitOnEscOrBack = EditorGUILayout.Toggle("Quit on [Esc].", arcontroller.QuitOnEscOrBack);
			if (arcontroller.QuitOnEscOrBack) {
				EditorGUILayout.HelpBox("The [esc] key (Windows, OS X) or the [Back] button (Android) will quit the app.", MessageType.Info);
			} else {
				EditorGUILayout.HelpBox("The [esc] key (Windows, OS X) or the [Back] button (Android) will be ignored.", MessageType.Warning);
			}

			arcontroller.VideoIsStereo = EditorGUILayout.Toggle("Stereo Video Input", arcontroller.VideoIsStereo);
			EditorGUILayout.HelpBox("Check this option if you plan to use two cameras to track the environment. This is not stereoscopic rendering. Note: You will have to configure both left (L) and right (R) cameras separately.", MessageType.Info);
		}
    }
}