using UnityEngine;

[System.Serializable]
public class Landmark
{
    public string name;              // Name of the landmark (e.g., "Statue of Liberty")
    public GameObject prefab;        // 3D model prefab for the landmark
    public Sprite icon;              // Icon for the UI button
    public Transform correctPosition; // Empty GameObject marking the correct position on the globe
    public string hint;              // Hint text (e.g., "Located in New York, USA")
    public float tolerance;          // Tolerance radius in world units (e.g., 0.5 units)
}