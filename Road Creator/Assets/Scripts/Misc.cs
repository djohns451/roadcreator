﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Misc{

	public static Vector3 Lerp3 (Vector3 start, Vector3 middle, Vector3 end, float time)
    {
        Vector3 startToMiddle = Vector3.Lerp(start, middle, time);
        return Vector3.Lerp(startToMiddle, end, time);
    }

    public static Vector3 Round (Vector3 toRound)
    {
        return new Vector3(Mathf.Round(toRound.x), Mathf.Round(toRound.y), Mathf.Round(toRound.z));
    }

    public static float CalculateDistance (Vector3 startPosition, Vector3 controlPosition, Vector3 endPosition)
    {
        float distance = 0;
        for (float t = 0.1f; t <= 1.1f; t += 0.1f)
        {
            distance += Vector3.Distance(Lerp3(startPosition, controlPosition, endPosition, t), Lerp3(startPosition, controlPosition, endPosition, t - 0.1f));
        }

        return distance;
    }

    public static Vector3 CalculateLeft (Vector3[] points, Vector3[] nextSegmentPoints, int index)
    {
        Vector3 forward;
        if (index < points.Length - 1)
        {
            forward = points[index + 1] - points[index];
        }
        else
        {
            // Last vertices
            if (nextSegmentPoints != null)
            {
                forward = nextSegmentPoints[1] - points[points.Length - 1];
            }
            else
            {
                forward = points[index] - points[index - 1];
            }
        }
        forward.Normalize();

        return new Vector3(-forward.z, 0, forward.x);
    }

    public static Vector3 GetCenter (Vector3 one, Vector3 two)
    {
        Vector3 difference = two - one;
        return (one + (difference / 2));
    }

}