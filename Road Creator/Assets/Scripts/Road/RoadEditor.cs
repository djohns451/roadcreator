﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoadCreator))]
public class RoadEditor : Editor
{

    RoadCreator roadCreator;
    Vector3[] points = null;
    GameObject objectToMove, extraObjectToMove;
    Tool lastTool;

    private void OnEnable()
    {
        roadCreator = (RoadCreator)target;
        if (roadCreator.transform.childCount == 0)
        {
            GameObject segments = new GameObject("Segments");
            segments.transform.SetParent(roadCreator.transform);
        }

        if (roadCreator.globalSettings == null)
        {
            if (GameObject.FindObjectOfType<GlobalSettings>() == null)
            {
                roadCreator.globalSettings = new GameObject("Global Settings").AddComponent<GlobalSettings>();
            } else
            {
                roadCreator.globalSettings = GameObject.FindObjectOfType<GlobalSettings>();
            }
        }

        if (roadCreator.defaultRoadMaterial == null) {
            roadCreator.defaultRoadMaterial = roadCreator.globalSettings.defaultRoadMaterial;
        }

        if (roadCreator.defaultShoulderMaterial == null)
        {
            roadCreator.defaultShoulderMaterial = roadCreator.globalSettings.defaultShoulderMaterial;
        }

        lastTool = Tools.current;
        Tools.current = Tool.None;
    }

    private void OnDisable()
    {
        Tools.current = lastTool;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Reset road"))
        {
            roadCreator.currentSegment = null;
            roadCreator.points.Clear();

            for (int i = roadCreator.transform.GetChild(0).childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(roadCreator.transform.GetChild(0).GetChild(i).gameObject);
            }
        }

        if (GUILayout.Button("Generate road"))
        {
            UpdateMesh();
        }
    }

    public void UpdateMesh()
    {
        Vector3[] currentPoints = null;

        for (int i = 0; i < roadCreator.transform.GetChild(0).childCount; i++)
        {
            if (roadCreator.transform.GetChild(0).GetChild(i).GetChild(0).childCount == 3)
            {
                if (i == 0)
                {
                    currentPoints = CalculatePoints(roadCreator.transform.GetChild(0).GetChild(i));
                }

                if (i < roadCreator.transform.GetChild(0).childCount - 1 && roadCreator.transform.GetChild(0).GetChild(i + 1).GetChild(0).childCount == 3)
                {
                    Vector3[] nextPoints = CalculatePoints(roadCreator.transform.GetChild(0).GetChild(i + 1));
                    Vector3 originalControlPoint = currentPoints[currentPoints.Length - 1];

                    int smoothnessAmount = roadCreator.smoothnessAmount;
                    if ((currentPoints.Length / 2) < smoothnessAmount)
                    {
                        smoothnessAmount = currentPoints.Length / 2;
                    }

                    if ((nextPoints.Length / 2) < smoothnessAmount)
                    {
                        smoothnessAmount = nextPoints.Length / 2;
                    }

                    float distanceSection = 1f / ((smoothnessAmount * 2));
                    for (float t = distanceSection; t < 0.5; t += distanceSection)
                    {
                        // First sectiond
                        currentPoints[currentPoints.Length - 1 - smoothnessAmount + (int)(t * 2 * smoothnessAmount)] = Misc.Lerp3(currentPoints[currentPoints.Length - 1 - smoothnessAmount], originalControlPoint, nextPoints[smoothnessAmount], t);

                        // Second section
                        nextPoints[smoothnessAmount - (int)(t * 2 * smoothnessAmount)] = Misc.Lerp3(currentPoints[currentPoints.Length - 1 - smoothnessAmount], originalControlPoint, nextPoints[smoothnessAmount], 1 - t);
                    }

                    // First and last points
                    currentPoints[currentPoints.Length - 1] = Misc.Lerp3(currentPoints[currentPoints.Length - 1 - smoothnessAmount], originalControlPoint, nextPoints[smoothnessAmount], 0.5f);
                    //currentPoints[currentPoints.Length - smoothnessAmount - 2] = Misc.GetCenter(currentPoints[currentPoints.Length - smoothnessAmount - 3], currentPoints[currentPoints.Length - smoothnessAmount - 1]);
                    nextPoints[0] = Misc.Lerp3(currentPoints[currentPoints.Length - 1 - smoothnessAmount], originalControlPoint, nextPoints[smoothnessAmount], 0.5f);

                    roadCreator.transform.GetChild(0).GetChild(i).GetComponent<RoadSegment>().CreateRoadMesh(currentPoints, nextPoints, roadCreator.heightOffset, roadCreator.transform.GetChild(0).GetChild(i), smoothnessAmount);
                    roadCreator.StartCoroutine(FixTextureStretch(Misc.CalculateDistance(roadCreator.transform.GetChild(0).GetChild(i).GetChild(0).GetChild(0).position, roadCreator.transform.GetChild(0).GetChild(i).GetChild(0).GetChild(1).position, roadCreator.transform.GetChild(0).GetChild(i).GetChild(0).GetChild(2).position), i));
                    currentPoints = nextPoints;
                }
                else
                {
                    roadCreator.transform.GetChild(0).GetChild(i).GetComponent<RoadSegment>().CreateRoadMesh(currentPoints, null, roadCreator.heightOffset, roadCreator.transform.GetChild(0).GetChild(i), 0);
                    roadCreator.StartCoroutine(FixTextureStretch(Misc.CalculateDistance(roadCreator.transform.GetChild(0).GetChild(i).GetChild(0).GetChild(0).position, roadCreator.transform.GetChild(0).GetChild(i).GetChild(0).GetChild(1).position, roadCreator.transform.GetChild(0).GetChild(i).GetChild(0).GetChild(2).position), i));
                }
            }
        }
    }

    IEnumerator FixTextureStretch(float length, int i)
    {
        yield return new WaitForSeconds(0.1f);
        float textureRepeat = length * roadCreator.globalSettings.resolution * 0.07f;

        for (int j = 0; j < 3; j++)
        {
            Material material = new Material(roadCreator.transform.GetChild(0).GetChild(i).GetChild(1).GetChild(j).GetComponent<MeshRenderer>().sharedMaterial);
            material.mainTextureScale = new Vector2(1, textureRepeat);
            roadCreator.transform.GetChild(0).GetChild(i).GetChild(1).GetChild(j).GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }

    public void OnSceneGUI()
    {
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        Event guiEvent = Event.current;

        Ray ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);

        RaycastHit raycastHit;
        if (Physics.Raycast(ray, out raycastHit, 100f, ~(1 << roadCreator.globalSettings.layer)))
        {
            Vector3 hitPosition = raycastHit.point;

            if (guiEvent.control == true)
            {
                hitPosition = Misc.Round(hitPosition);
            }

            if (guiEvent.type == EventType.MouseDown)
            {
                if (guiEvent.button == 0)
                {
                    if (guiEvent.shift == true)
                    {
                        CreatePoints(hitPosition);
                    }
                }
                else if (guiEvent.button == 1 && guiEvent.shift == true)
                {
                    RemovePoints();
                }
            }

            if (roadCreator.currentSegment != null && roadCreator.currentSegment.transform.GetChild(0).childCount == 2 && (guiEvent.type == EventType.MouseDrag || guiEvent.type == EventType.MouseMove || guiEvent.type == EventType.MouseDown))
            {
                points = CalculatePoints(guiEvent, hitPosition);
            }

            if (roadCreator.transform.childCount > 0)
            {
                Draw(guiEvent, hitPosition);
            }
        }

        if (Physics.Raycast(ray, out raycastHit, 100f))
        {
            Vector3 hitPosition = raycastHit.point;

            if (guiEvent.control == true)
            {
                hitPosition = Misc.Round(hitPosition);
            }

            if (guiEvent.shift == false)
            {
                MovePoints(guiEvent, raycastHit, hitPosition);
            }
        }
    }

    private void CreatePoints(Vector3 hitPosition)
    {
        if (roadCreator.currentSegment != null)
        {
            if (roadCreator.currentSegment.transform.GetChild(0).childCount == 1)
            {
                // Create control point
                Undo.RecordObject(CreatePoint("Control Point", roadCreator.currentSegment.transform.GetChild(0), hitPosition), "Create Point");
            }
            else if (roadCreator.currentSegment.transform.GetChild(0).childCount == 2)
            {
                // Create end point
                Undo.RecordObject(CreatePoint("End Point", roadCreator.currentSegment.transform.GetChild(0), hitPosition), "Create Point");
                roadCreator.currentSegment = null;
                UpdateMesh();
            }
        }
        else
        {
            if (roadCreator.transform.GetChild(0).childCount == 0)
            {
                // Create first segment
                RoadSegment segment = CreateSegment(hitPosition);
                CreatePoint("Start Point", segment.transform.GetChild(0), hitPosition);
            }
            else
            {
                RoadSegment segment = CreateSegment(roadCreator.transform.GetChild(0).GetChild(roadCreator.transform.GetChild(0).childCount - 1).GetChild(0).GetChild(2).position);
                CreatePoint("Start Point", segment.transform.GetChild(0), segment.transform.position);
                CreatePoint("Control Point", segment.transform.GetChild(0), hitPosition);
            }
        }
    }

    private GameObject CreatePoint(string name, Transform parent, Vector3 position)
    {
        GameObject point = new GameObject(name);
        point.gameObject.AddComponent<BoxCollider>();
        point.GetComponent<BoxCollider>().size = new Vector3(roadCreator.globalSettings.pointSize, roadCreator.globalSettings.pointSize, roadCreator.globalSettings.pointSize);
        point.transform.SetParent(parent);
        point.transform.position = position;
        point.layer = roadCreator.globalSettings.layer;
        roadCreator.points.Add(point);
        return point;
    }

    private RoadSegment CreateSegment(Vector3 position)
    {
        RoadSegment segment = new GameObject("Segment").AddComponent<RoadSegment>();
        segment.transform.SetParent(roadCreator.transform.GetChild(0), false);
        segment.transform.position = position;
        segment.roadMaterial = roadCreator.defaultRoadMaterial;
        segment.leftShoulderMaterial = roadCreator.defaultShoulderMaterial;
        segment.rightShoulderMaterial = roadCreator.defaultShoulderMaterial;

        GameObject points = new GameObject("Points");
        points.transform.SetParent(segment.transform);
        points.transform.localPosition = Vector3.zero;

        GameObject meshes = new GameObject("Meshes");
        meshes.transform.SetParent(segment.transform);
        meshes.transform.localPosition = Vector3.zero;

        GameObject mainMesh = new GameObject("Main Mesh");
        mainMesh.transform.SetParent(meshes.transform);
        mainMesh.transform.localPosition = Vector3.zero;
        mainMesh.AddComponent<MeshRenderer>();
        mainMesh.AddComponent<MeshFilter>();
        mainMesh.AddComponent<MeshCollider>();

        GameObject leftShoulderMesh = new GameObject("Left Shoulder Mesh");
        leftShoulderMesh.transform.SetParent(meshes.transform);
        leftShoulderMesh.transform.localPosition = Vector3.zero;
        leftShoulderMesh.AddComponent<MeshRenderer>();
        leftShoulderMesh.AddComponent<MeshFilter>();
        leftShoulderMesh.AddComponent<MeshCollider>();

        GameObject rightShoulderMesh = new GameObject("Right Shoulder Mesh");
        rightShoulderMesh.transform.SetParent(meshes.transform);
        rightShoulderMesh.transform.localPosition = Vector3.zero;
        rightShoulderMesh.AddComponent<MeshRenderer>();
        rightShoulderMesh.AddComponent<MeshFilter>();
        rightShoulderMesh.AddComponent<MeshCollider>();

        roadCreator.currentSegment = segment;

        return segment;
    }

    private void MovePoints(Event guiEvent, RaycastHit raycastHit, Vector3 hitPosition)
    {
        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && objectToMove == null)
        {
            if (raycastHit.collider.gameObject.name == "Control Point")
            {
                objectToMove = raycastHit.collider.gameObject;
                objectToMove.GetComponent<BoxCollider>().enabled = false;
            }
            else if (raycastHit.collider.gameObject.name == "Start Point")
            {
                objectToMove = raycastHit.collider.gameObject;
                objectToMove.GetComponent<BoxCollider>().enabled = false;

                if (objectToMove.transform.parent.parent.GetSiblingIndex() > 0)
                {
                    extraObjectToMove = raycastHit.collider.gameObject.transform.parent.parent.parent.GetChild(objectToMove.transform.parent.parent.GetSiblingIndex() - 1).GetChild(0).GetChild(2).gameObject;
                    extraObjectToMove.GetComponent<BoxCollider>().enabled = false;
                }
            }
            else if (raycastHit.collider.gameObject.name == "End Point")
            {
                objectToMove = raycastHit.collider.gameObject;
                objectToMove.GetComponent<BoxCollider>().enabled = false;

                if (objectToMove.transform.parent.parent.GetSiblingIndex() < objectToMove.transform.parent.parent.parent.childCount - 1 && raycastHit.collider.gameObject.transform.parent.parent.parent.GetChild(objectToMove.transform.parent.parent.GetSiblingIndex() + 1).GetChild(0).childCount == 3)
                {
                    extraObjectToMove = raycastHit.collider.gameObject.transform.parent.parent.parent.GetChild(objectToMove.transform.parent.parent.GetSiblingIndex() + 1).GetChild(0).GetChild(0).gameObject;
                    extraObjectToMove.GetComponent<BoxCollider>().enabled = false;
                }
            }
        }
        else if (guiEvent.type == EventType.MouseDrag && objectToMove != null)
        {
            Undo.RecordObject(objectToMove, "Moved Point");
            objectToMove.transform.position = hitPosition;

            if (extraObjectToMove != null)
            {
                extraObjectToMove.transform.position = hitPosition;
            }
        }
        else if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0 && objectToMove != null)
        {
            objectToMove.GetComponent<BoxCollider>().enabled = true;
            objectToMove = null;

            if (extraObjectToMove != null)
            {
                extraObjectToMove.GetComponent<BoxCollider>().enabled = true;
                extraObjectToMove = null;
            }

            UpdateMesh();
        }
    }

    private void RemovePoints ()
    {
        if (roadCreator.transform.GetChild(0).childCount > 0)
        {
            if (roadCreator.currentSegment != null)
            {
                if (roadCreator.currentSegment.transform.GetChild(0).childCount == 2)
                {
                    DestroyImmediate(roadCreator.currentSegment.transform.GetChild(0).GetChild(1).gameObject);
                    roadCreator.points.Remove(roadCreator.points[roadCreator.points.Count - 1]);
                } else if (roadCreator.currentSegment.transform.GetChild(0).childCount == 1)
                {
                    DestroyImmediate(roadCreator.currentSegment.gameObject);
                    roadCreator.points.Remove(roadCreator.points[roadCreator.points.Count - 1]);

                    if (roadCreator.transform.GetChild(0).childCount > 0)
                    {
                        roadCreator.transform.GetChild(0).GetChild(roadCreator.transform.GetChild(0).childCount - 1).GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh = null;
                        roadCreator.transform.GetChild(0).GetChild(roadCreator.transform.GetChild(0).childCount - 1).GetChild(1).GetChild(0).GetComponent<MeshCollider>().sharedMesh = null;
                        DestroyImmediate(roadCreator.transform.GetChild(0).GetChild(roadCreator.transform.GetChild(0).childCount - 1).GetChild(0).GetChild(2).gameObject);
                        roadCreator.points.Remove(roadCreator.points[roadCreator.points.Count - 1]);
                        roadCreator.currentSegment = roadCreator.transform.GetChild(0).GetChild(roadCreator.transform.GetChild(0).childCount - 1).GetComponent<RoadSegment>();
                    } else
                    {
                        roadCreator.currentSegment = null;
                    }
                }
            } else
            {
                roadCreator.transform.GetChild(0).GetChild(roadCreator.transform.GetChild(0).childCount - 1).GetChild(1).GetChild(0).GetComponent<MeshFilter>().sharedMesh = null;
                roadCreator.transform.GetChild(0).GetChild(roadCreator.transform.GetChild(0).childCount - 1).GetChild(1).GetChild(0).GetComponent<MeshCollider>().sharedMesh = null;
                DestroyImmediate(roadCreator.transform.GetChild(0).GetChild(roadCreator.transform.GetChild(0).childCount - 1).GetChild(0).GetChild(2).gameObject);
                roadCreator.points.Remove(roadCreator.points[roadCreator.points.Count - 1]);
                roadCreator.currentSegment = roadCreator.transform.GetChild(0).GetChild(roadCreator.transform.GetChild(0).childCount - 1).GetComponent<RoadSegment>();
            }
        }
    }

    private void Draw(Event guiEvent, Vector3 hitPosition)
    {
        // Mouse position
        Handles.color = Color.blue;
        Handles.CylinderHandleCap(0, hitPosition, Quaternion.Euler(90, 0, 0), roadCreator.globalSettings.pointSize, EventType.Repaint);

        if (roadCreator.points != null)
        {
            for (int i = 0; i < roadCreator.points.Count; i++)
            {
                if (roadCreator.points[i].name != "Control Point")
                {
                    Handles.color = Color.red;
                    Handles.CylinderHandleCap(0, roadCreator.points[i].transform.position, Quaternion.Euler(90, 0, 0), roadCreator.globalSettings.pointSize, EventType.Repaint);
                }
                else
                {
                    Handles.color = Color.yellow;
                    Handles.CylinderHandleCap(0, roadCreator.points[i].transform.position, Quaternion.Euler(90, 0, 0), roadCreator.globalSettings.pointSize, EventType.Repaint);
                }
            }
        }

        for (int j = 0; j < roadCreator.transform.GetChild(0).childCount; j++)
        {
            if (roadCreator.transform.GetChild(0).GetChild(j).GetChild(0).childCount == 3)
            {
                Handles.color = Color.white;
                Handles.DrawLine(roadCreator.transform.GetChild(0).GetChild(j).GetChild(0).GetChild(0).position, roadCreator.transform.GetChild(0).GetChild(j).GetChild(0).GetChild(1).position);
                Handles.DrawLine(roadCreator.transform.GetChild(0).GetChild(j).GetChild(0).GetChild(1).position, roadCreator.transform.GetChild(0).GetChild(j).GetChild(0).GetChild(2).position);
            }
            else if (roadCreator.transform.GetChild(0).GetChild(j).GetChild(0).childCount == 2)
            {
                Handles.color = Color.white;
                Handles.DrawLine(roadCreator.transform.GetChild(0).GetChild(j).GetChild(0).GetChild(0).position, roadCreator.transform.GetChild(0).GetChild(j).GetChild(0).GetChild(1).position);

                if (guiEvent.shift == true)
                {
                    Handles.DrawLine(roadCreator.transform.GetChild(0).GetChild(j).GetChild(0).GetChild(1).position, hitPosition);
                }

                if (points != null && guiEvent.shift == true)
                {
                    Handles.color = Color.black;
                    Handles.DrawPolyLine(points);
                }
            } else
            {
                Handles.color = Color.black;
                Handles.DrawLine(roadCreator.transform.GetChild(0).GetChild(roadCreator.transform.GetChild(0).childCount - 1).GetChild(0).GetChild(0).position, hitPosition);
            }
        }
        
        if (roadCreator.currentSegment == null && roadCreator.transform.GetChild(0).childCount > 0 && roadCreator.transform.GetChild(0).GetChild(roadCreator.transform.GetChild(0).childCount - 1).GetChild(0).childCount == 3)
        {
            if (guiEvent.shift == true)
            {
                Handles.color = Color.black;
                Handles.DrawLine(roadCreator.transform.GetChild(0).GetChild(roadCreator.transform.GetChild(0).childCount - 1).GetChild(0).GetChild(2).position, hitPosition);
            }
        }
    }

    public Vector3[] CalculatePoints(Transform segment)
    {
        float divisions = Misc.CalculateDistance(segment.GetChild(0).GetChild(0).position, segment.GetChild(0).GetChild(1).position, segment.GetChild(0).GetChild(2).position);
        divisions = Mathf.Max(2, divisions);
        List<Vector3> points = new List<Vector3>();
        float distancePerDivision = 1 / divisions;
        
        for (float t = 0; t <= 1; t += distancePerDivision)
        {
            if (t > 1 - distancePerDivision)
            {
                t = 1;
            }

            Vector3 position = Misc.Lerp3(segment.GetChild(0).GetChild(0).position, segment.GetChild(0).GetChild(1).position, segment.GetChild(0).GetChild(2).position, t);

            RaycastHit raycastHit;
            if (Physics.Raycast(position, Vector3.down, out raycastHit, 100f, ~(1 << roadCreator.globalSettings.layer)))
            {
                position.y = raycastHit.point.y;
            }

            points.Add(position);
        }

        return points.ToArray();
    }

    private Vector3[] CalculatePoints(Event guiEvent, Vector3 hitPosition)
    {
        float divisions = Misc.CalculateDistance(roadCreator.currentSegment.transform.GetChild(0).GetChild(0).position, roadCreator.currentSegment.transform.GetChild(0).GetChild(1).position, hitPosition);
        divisions = Mathf.Max(2, divisions);
        List<Vector3> points = new List<Vector3>();
        float distancePerDivision = 1 / divisions;

        for (float t = 0; t <= 1; t += distancePerDivision)
        {
            if (t > 1 - distancePerDivision)
            {
                t = 1;
            }

            Vector3 position = Misc.Lerp3(roadCreator.currentSegment.transform.GetChild(0).GetChild(0).position, roadCreator.currentSegment.transform.GetChild(0).GetChild(1).position, hitPosition, t);

            RaycastHit raycastHit;
            if (Physics.Raycast(position, Vector3.down, out raycastHit, 100f, ~(1 << roadCreator.globalSettings.layer)))
            {
                position.y = raycastHit.point.y;
            }

            points.Add(position);
        }

        return points.ToArray();
    }
}