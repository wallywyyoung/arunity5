/*
 *  ARTrackedCamera.cs
 *  ARToolKit for Unity
 *
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
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// A class which directly associates an ARMarker with a Unity Camera object.
/// This class responds to AAREventReciever, and can be set as an Event Reciever on an ARMarker object.
/// This will take the inverse of that marker's pose, and set this object (the camera) to it.
/// </summary>
public class ARTrackedCamera : ARTrackedObject {
	private Coroutine timer                  = null;

	// TODO: Handle the association of the marker on Start which disables the child components.
	public override void OnMarkerFound(ARTrackedMarker marker) {
		if (null != timer) {
			StopCoroutine(timer);
			timer = null;
			return;
		}
		for (int i = 0; i < marker.transform.childCount; ++i) {
			marker.transform.GetChild(i).gameObject.SetActive(true);
		}
	}
	
	public override void OnMarkerLost(ARTrackedMarker marker) {
		if (null != timer) {
			return;
		}
		timer = StartCoroutine(MarkerLostTimer(marker));
	}
	
	public override void OnMarkerTracked(ARTrackedMarker marker) {
		// 4 - If visible, set marker pose.
		Vector3 storedScale = transform.localScale;
		Matrix4x4 pose = marker.transform.localToWorldMatrix * marker.TransformationMatrix.inverse;
		transform.position   = ARUtilityFunctions.PositionFromMatrix(pose);
		transform.rotation   = ARUtilityFunctions.RotationFromMatrix(pose);
		transform.localScale = storedScale;
	}
	
	private IEnumerator MarkerLostTimer(ARTrackedMarker marker) {
		yield return new WaitForSeconds(secondsToRemainVisible);
		for (int i = 0; i < marker.transform.childCount; ++i) {
			marker.transform.GetChild(i).gameObject.SetActive(false);
		}
		timer = null;
	}
}
