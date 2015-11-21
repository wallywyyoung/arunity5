/*
 *  ARTrackedObject.cs
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
 *  Author(s): Philip Lamb, Wally Young
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[RequireComponent(typeof(Transform))]
[ExecuteInEditMode]
public class ARTrackedObject : AAREventReciever {
	private Coroutine timer                  = null;
	public  float     secondsToRemainVisible = 0.0f;	// After tracking is lost (to reduce flicker).
	
	public override void OnMarkerFound(ARTrackedMarker marker) {
		if (null != timer) {
			StopCoroutine(timer);
			timer = null;
			return;
		}
		for (int i = 0; i < transform.childCount; ++i) {
			Debug.LogError(transform.GetChild(i).name);
			transform.GetChild(i).gameObject.SetActive(true);
		}
	}
	
	public override void OnMarkerLost(ARTrackedMarker marker) {
		if (null != timer) {
			return;
		}
		timer = StartCoroutine(MarkerLostTimer());
	}
	
	public override void OnMarkerTracked(ARTrackedMarker marker) {
		// 4 - If visible, set marker pose.
		Vector3 storedScale = transform.localScale;
		Matrix4x4 pose;
		pose = ARStaticCamera.Instance.transform.localToWorldMatrix * marker.TransformationMatrix;
		transform.position   = ARUtilityFunctions.PositionFromMatrix(pose);
		transform.rotation   = ARUtilityFunctions.RotationFromMatrix(pose);
		transform.localScale = storedScale;
	}

	private void Start()	{
		if (!Application.isPlaying) {
			return;
		}
		// In Player, set initial visibility to not visible.
		for (int i = 0; i < transform.childCount; ++i) {
			this.transform.GetChild(i).gameObject.SetActive(false);
		}
	}

	private IEnumerator MarkerLostTimer() {
		yield return new WaitForSeconds(secondsToRemainVisible);
		for (int i = 0; i < transform.childCount; ++i) {
			transform.GetChild(i).gameObject.SetActive(false);
		}
		timer = null;
	}
}

