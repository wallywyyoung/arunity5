/*
 *  ARMarkerEditor.cs
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
using System.IO;

[CustomEditor(typeof(ARMarker))]
public class ARMarkerEditor : Editor {
    public bool showFilterOptions = false;
	public bool showGlobalSquareOptions = false;

    public override void OnInspectorGUI() {
        EditorGUILayout.BeginVertical();
		
		// Get the ARMarker that this panel will edit.
        ARMarker arMarker = (ARMarker)target;
        if (null == arMarker) {
			return;
		}
		
		// Attempt to load. Might not work out if e.g. for a single marker, pattern hasn't been
		// assigned yet, or for an NFT marker, dataset hasn't been specified.
		if (arMarker.UID == ARMarker.NO_ID) {
			arMarker.Load(); 
		}
		
		// Marker tag
        arMarker.Tag = EditorGUILayout.TextField("Tag", arMarker.Tag);
        EditorGUILayout.LabelField("Unique ID", (arMarker.UID == ARMarker.NO_ID ? "Not Loaded": arMarker.UID.ToString()));
		
        EditorGUILayout.Separator();
			
		arMarker.MarkerType = (MarkerType)EditorGUILayout.EnumPopup("Marker Type", arMarker.MarkerType);
		
		// Description of the type of marker
        EditorGUILayout.LabelField("Description", ARMarker.MarkerTypeNames[arMarker.MarkerType]);
        switch (arMarker.MarkerType) {
			case MarkerType.Square:
				if (ARToolKitAssetManager.PatternMarkers.Length > 0) {
					int    patternFilenameIndex = EditorGUILayout.Popup("Pattern File", arMarker.FilenameIndex, ARToolKitAssetManager.PatternMarkers);
					string patternFilename      = ARToolKitAssetManager.PatternMarkers[patternFilenameIndex];
					if (patternFilename != arMarker.PatternFilename) {
						string patternContents = GetPatternContents(patternFilename);
						arMarker.FilenameIndex = patternFilenameIndex;
						arMarker.SetPatternProperties(patternFilename, patternContents);
					}
				} else {
					EditorGUILayout.LabelField("Pattern File", "No patterns available.");
					arMarker.SetPatternProperties(string.Empty, string.Empty);
					arMarker.FilenameIndex = 0;
				}
				arMarker.PatternWidth          = EditorGUILayout.FloatField("Pattern Width (m)",         arMarker.PatternWidth);
				arMarker.UseContPoseEstimation = EditorGUILayout.Toggle(    "Contstant Pose Estimation", arMarker.UseContPoseEstimation);
				break;
        	case MarkerType.SquareBarcode:
				arMarker.BarcodeID             = EditorGUILayout.IntField(  "Barcode ID",                arMarker.BarcodeID);
				arMarker.PatternWidth          = EditorGUILayout.FloatField("Pattern Width (m)",         arMarker.PatternWidth);
				arMarker.UseContPoseEstimation = EditorGUILayout.Toggle(    "Contstant Pose Estimation", arMarker.UseContPoseEstimation);
				break;
			case MarkerType.Multimarker:
				if (ARToolKitAssetManager.PatternMarkers.Length > 0) {
					int    multiFilenameIndex = EditorGUILayout.Popup("Multimarker File", arMarker.FilenameIndex, ARToolKitAssetManager.Multimarkers);
					string multiFilename      = ARToolKitAssetManager.PatternMarkers[multiFilenameIndex];
					if (multiFilename != arMarker.PatternFilename) {
						arMarker.FilenameIndex   = multiFilenameIndex;
						arMarker.MultiConfigFile = multiFilename;
					}
				} else {
					EditorGUILayout.LabelField("Multimarker File", "No multimarkers available.");
					arMarker.MultiConfigFile = string.Empty;
					arMarker.FilenameIndex = 0;
				}
        	    break;
			case MarkerType.NFT:
				if (ARToolKitAssetManager.PatternMarkers.Length > 0) {
					int    nftIndex    = EditorGUILayout.Popup("NFT File", arMarker.FilenameIndex, ARToolKitAssetManager.NFTMarkers);
					string nftFilename = ARToolKitAssetManager.PatternMarkers[nftIndex];
					if (nftFilename != arMarker.PatternFilename) {
						arMarker.FilenameIndex   = nftIndex;
						arMarker.NFTDataName = nftFilename;
					}
				} else {
					EditorGUILayout.LabelField("NFT File", "No NFT markers available.");
					arMarker.FilenameIndex = 0;
					arMarker.NFTDataName = string.Empty;
				}
				float nftScale = EditorGUILayout.FloatField("NFT Marker scalefactor", arMarker.NFTScale);
				if (nftScale != arMarker.NFTScale) {
					EditorUtility.SetDirty(arMarker);
				}
				arMarker.NFTScale = nftScale;
				break;
        }

        EditorGUILayout.Separator();
		
		arMarker.Filtered = EditorGUILayout.Toggle("Filter Pose", arMarker.Filtered);
		if (arMarker.Filtered) {
			arMarker.FilterSampleRate = EditorGUILayout.Slider("Sample rate:", arMarker.FilterSampleRate, 1.0f, 30.0f);
			arMarker.FilterCutoffFreq = EditorGUILayout.Slider("Cutoff freq.:", arMarker.FilterCutoffFreq, 1.0f, 30.0f);
		}

        EditorGUILayout.BeginHorizontal();

        // Draw all the marker images
        if (arMarker.Patterns != null) {
            for (int i = 0; i < arMarker.Patterns.Length; ++i) {
				GUILayout.Label(new GUIContent(string.Format("Pattern {0}, {1}m", i, arMarker.Patterns[i].width.ToString("n3")), arMarker.Patterns[i].texture), GUILayout.ExpandWidth(false)); // n3 -> 3 decimal places.
            }
        }
		
        EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();

		if (arMarker.MarkerType == MarkerType.Square || arMarker.MarkerType == MarkerType.SquareBarcode || arMarker.MarkerType == MarkerType.Multimarker) {
			showGlobalSquareOptions = EditorGUILayout.Foldout(showGlobalSquareOptions, "Global Square Tracking Options");
			if (showGlobalSquareOptions) {
				ARController.Instance.TemplateSize = EditorGUILayout.IntSlider("Template Size (bits)", ARController.Instance.TemplateSize, 16, 64);
				
				int currentTemplateCountMax = ARController.Instance.TemplateCountMax;
				int newTemplateCountMax = EditorGUILayout.IntField("Maximum Template Count", currentTemplateCountMax);
				if (newTemplateCountMax != currentTemplateCountMax && newTemplateCountMax > 0) {
					ARController.Instance.TemplateCountMax = newTemplateCountMax;
				}
				
				bool trackInColor = EditorGUILayout.Toggle("Track Templates in Color", ARController.Instance.trackTemplatesInColor);
				if (trackInColor != ARController.Instance.trackTemplatesInColor) {
					ARController.Instance.trackTemplatesInColor = trackInColor;
					UpdatePatternDetectionMode();
				}
				
				ARController.Instance.BorderSize    = UnityEngine.Mathf.Clamp(EditorGUILayout.FloatField("Border Size (%)", ARController.Instance.BorderSize), 0.0f, 0.5f);
				ARController.Instance.LabelingMode  = (ARController.ARToolKitLabelingMode)EditorGUILayout.EnumPopup("Marker Border Color", ARController.Instance.LabelingMode);
				ARController.Instance.ImageProcMode = (ARController.ARToolKitImageProcMode)EditorGUILayout.EnumPopup("Image Processing Mode", ARController.Instance.ImageProcMode); 
			}
		}
	}
	
	private static void UpdatePatternDetectionMode() {
		ARMarker[] markers = FindObjectsOfType<ARMarker>();
		
		bool trackColor = ARController.Instance.trackTemplatesInColor;
		bool templateMarkers = false;
		bool matrixMarkers   = false;
		foreach (ARMarker marker in markers) {
			switch (marker.MarkerType) {
			case MarkerType.Multimarker:
				// Dumb default, pending introspection into dat file.
				templateMarkers = true;
				matrixMarkers = true;
				break;
			case MarkerType.Square:
				templateMarkers = true;
				break;
			case MarkerType.SquareBarcode:
				matrixMarkers = true;
				break;
			}
		}
		
		var mode = ARController.ARToolKitPatternDetectionMode.AR_MATRIX_CODE_DETECTION;
		if (templateMarkers && matrixMarkers) {
			if (trackColor) {
				mode = ARController.ARToolKitPatternDetectionMode.AR_TEMPLATE_MATCHING_COLOR_AND_MATRIX;
			} else {
				mode = ARController.ARToolKitPatternDetectionMode.AR_TEMPLATE_MATCHING_MONO_AND_MATRIX;
			}
		} else if (templateMarkers && !matrixMarkers) {
			if (trackColor) {
				mode = ARController.ARToolKitPatternDetectionMode.AR_TEMPLATE_MATCHING_COLOR;
			} else {
				mode = ARController.ARToolKitPatternDetectionMode.AR_TEMPLATE_MATCHING_MONO;
			}
		}
		
		ARController.Instance.PatternDetectionMode = mode;
	}

	private const string PATTERN_EXT = ".patt";
	private static string GetPatternContents(string markerName) {
		string path = Path.Combine(Application.streamingAssetsPath, ARToolKitAssetManager.PATTERN_DIRECTORY_NAME);
		path = Path.Combine(path, markerName + PATTERN_EXT);
		return File.ReadAllText(path);
	}
}