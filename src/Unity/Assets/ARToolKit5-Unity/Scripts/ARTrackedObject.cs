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
	private Coroutine timer   = null;
	private AROrigin  _origin = null;

	public  float      secondsToRemainVisible = 0.0f;	// How long to remain visible after tracking is lost (to reduce flicker)

	// Return the origin associated with this component.
	// Uses cached value if available, otherwise performs a find operation.
	public virtual AROrigin GetOrigin()	{
		if (_origin == null) {
			// Locate the origin in parent.
			_origin = this.gameObject.GetComponentInParent<AROrigin>(); // Unity v4.5 and later.
		}
		return _origin;
	}

	void Start()	{
		if (!Application.isPlaying) {
			return;
		}
		// In Player, set initial visibility to not visible.
		for (int i = 0; i < transform.childCount; ++i) {
			this.transform.GetChild(i).gameObject.SetActive(false);
		}
	}

	public override void OnMarkerFound(ARTrackedMarker marker) {
		Debug.LogError("OnMarkerFound");
		if (null != timer) {
			Debug.LogError("null != timer");
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
		AROrigin origin = GetOrigin();
		if (null == origin) {
			return;
		}
		ARMarker baseMarker = origin.GetBaseMarker();
		if (null == baseMarker) {
			return;
		}
		
		Matrix4x4 pose;
		if (marker == baseMarker) {
			// If this marker is the base, no need to take base inverse etc.
			pose = origin.transform.localToWorldMatrix;
		} else {
			pose = (origin.transform.localToWorldMatrix * baseMarker.TransformationMatrix.inverse * marker.TransformationMatrix);
		}
		transform.position = ARUtilityFunctions.PositionFromMatrix(pose);
		transform.rotation = ARUtilityFunctions.RotationFromMatrix(pose);
	}

	private IEnumerator MarkerLostTimer() {
		yield return new WaitForSeconds(secondsToRemainVisible);
		for (int i = 0; i < transform.childCount; ++i) {
			transform.GetChild(i).gameObject.SetActive(false);
		}
		timer = null;
	}

}

