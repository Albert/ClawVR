﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ClawVR_InteractionManager : MonoBehaviour {
    public bool canSelectAnyObject = true;
	public GameObject subject { set; get; }
    private Transform subjectPreviousParent;
    private List<ClawVR_ClawController> clawControllers = new List<ClawVR_ClawController>();
	private List<GameObject> focalPoints = new List<GameObject>();
	private bool anyPointChangedThisFrame;
	private Vector3[] oldPointsForRotation = new Vector3[2];
	private Light selectionHighlighter;

	void Start () {
		subject = GameObject.Find ("Cube");
		selectionHighlighter = FindObjectOfType<Light> ();
	}

    void Update () {
        // a nice-to-have would be when canSelectAnyObject gets updated, repopulates throughout
    }

    void LateUpdate() {
        updatePointsIfNecessary();
        if (anyPointChangedThisFrame && subject.transform.parent == transform) {
            // kick it out for one frame
            subject.transform.SetParent(subjectPreviousParent);
        }
        applyTranslation ();
		applyScale ();
		applyRotation ();
        if (anyPointChangedThisFrame) {
            subjectPreviousParent = subject.transform.parent;
            subject.transform.SetParent(transform);
            if (focalPoints.Count == 0) {
                subject.transform.SetParent(subjectPreviousParent);
            }
        }
        runHighlighter ();
    }

    void updatePointsIfNecessary() {
		anyPointChangedThisFrame = false;
        foreach (ClawVR_ClawController clawController in clawControllers) {
			if (clawController.CheckIfPointsHaveUpdated()) {
				anyPointChangedThisFrame = true;
				updatePoints ();
                break; // only need to do this foreach loop once if points are updated
            }
        }
    }

	void updatePoints() {
		focalPoints.Clear();
		int controllersThatAreClosed = 0;
		foreach (ClawVR_ClawController clawController in clawControllers) {
			if (clawController.isClosed) {
				controllersThatAreClosed++;
			}
		}
		foreach (ClawVR_ClawController clawController in clawControllers) {
			if (controllersThatAreClosed == 1) {
				foreach (Transform child in clawController.gameObject.transform) {
					if (child.name == "ClawFocalPoint") {
						focalPoints.Add(child.gameObject);
					}
				}
			} else if (controllersThatAreClosed > 1) {
				// only add 1st focal point for each controller
				focalPoints.Add (clawController.gameObject.transform.Find ("ClawFocalPoint").gameObject);
			}
		}
	}

	void applyTranslation () {
		if (focalPoints.Count > 0) {
//			if (subjectHandlerSettings.lockTranslation) {
//				transform.position = subject.transform.position;
//			} else {
				Vector3 averagePoint = new Vector3();
				foreach (GameObject point in focalPoints) {
					averagePoint += point.transform.position;
				}
				averagePoint /= focalPoints.Count;
				transform.position = averagePoint;
//			}
		}
	}

	void applyScale () {
//		if (focalPoints.Count > 1 && !subjectHandlerSettings.lockScale) {
		if (focalPoints.Count > 1) {
			float averageDistance = 0;
			foreach (GameObject pointA in focalPoints) {
				foreach (GameObject pointB in focalPoints) {
					if (pointA != pointB) {
						averageDistance += (pointA.transform.position - pointB.transform.position).magnitude;
					}
				}
			}
			averageDistance /= focalPoints.Count;
			transform.localScale = new Vector3 (averageDistance, averageDistance, averageDistance);
		}
	}

	void applyRotation () {
//		if (focalPoints.Count == 2 && !subjectHandlerSettings.lockRotation) {
		if (focalPoints.Count == 2) {
			Vector3 direction1 = oldPointsForRotation[0] - oldPointsForRotation[1];
			Vector3 direction2 = focalPoints [0].transform.position - focalPoints [1].transform.position;
			Vector3 cross = Vector3.Cross (direction1, direction2);
			float amountToRot = Vector3.Angle (direction1, direction2);
			transform.RotateAround(transform.position, cross.normalized, amountToRot);

			oldPointsForRotation [0] = focalPoints [0].transform.position;
			oldPointsForRotation [1] = focalPoints [1].transform.position;
//		} else if (focalPoints.Count == 3 && !subjectHandlerSettings.lockRotation) {
		} else if (focalPoints.Count == 3) {
			// TODO Talk to a proper comp sci person about a better way to do this...
			Vector3 directionToLook = focalPoints [1].transform.position - focalPoints [0].transform.position;
			transform.LookAt (transform.position + directionToLook);
			Vector3 referenceDirection = focalPoints [2].transform.position - focalPoints [0].transform.position;
			Vector3 projectedReference = Vector3.ProjectOnPlane (referenceDirection, transform.forward);
			float levelingAngle = Vector3.Angle (transform.right, projectedReference);
			float sign = Vector3.Cross(transform.InverseTransformDirection(transform.right), projectedReference).z < 0 ? -1 : 1;
			levelingAngle *= sign;
			transform.Rotate (transform.InverseTransformDirection(transform.forward), levelingAngle);
//		} else if (focalPoints.Count > 3 && !subjectHandlerSettings.lockRotation) {
		} else if (focalPoints.Count > 3) {
			// TODO I have no idea how to solve for this
		}
	}

	void runHighlighter() {
		if (subject) {
			Collider col = subject.GetComponent<Collider> ();
			float mag = col.bounds.extents.magnitude;
			float animLength = 3.0f;
			float t = (Time.time % animLength);
			selectionHighlighter.range = Mathf.Lerp (mag / 2.0f, mag * 4.0f, t / animLength);
			selectionHighlighter.intensity = Mathf.Lerp (3, 0, t / animLength);
			selectionHighlighter.gameObject.transform.position = subject.transform.position;
		}
	}

    public void changeSubject(GameObject newSubject) {
        subject = newSubject;
    }

	public void registerClaw(ClawVR_ClawController c) {
		clawControllers.Add (c);
        c.ixdManager = this;
	}
}