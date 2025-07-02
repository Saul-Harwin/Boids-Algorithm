using UnityEngine;
using System.Collections.Generic;

public class Boid : MonoBehaviour {
    public Vector3 position;
    public Vector3 velocity;
    
    GameObject volume;
    Vector3 size;

    void Start() {
        this.velocity = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), 0);
    }

    List<Vector2> ClosestWallPoints() {
        this.volume = GameObject.Find("Volume");
        this.size = volume.GetComponent<BoxCollider2D>().size;
        this.position = transform.position;

        List<Vector4> walls = volume.GetComponent<Volume>().walls;
        List<Vector2> intersects = new List<Vector2>();

        for (int i = 0; i < walls.Count; i++) {
            Vector2 temp = Vector2.Perpendicular(new Vector2(walls[i][0], walls[i][1])).normalized;
            Vector4 perpLine = new Vector4(temp[0], temp[1], position[0], position[1]);

            Vector2 intersect = FindIntersection(walls[i], perpLine);
            intersects.Add(intersect);

            DrawLine(position, intersect);
        }

        return intersects;
    }

    Vector2 FindIntersection(Vector4 line1, Vector4 line2) {
        float a1 = line1[0], b1 = -line2[0];
        float a2 = line1[1], b2 = -line2[1];

        float det = a1 * b2 - b1 * a2;
        Vector2 rhs = new Vector2(line2[2] - line1[2], line2[3] - line1[3]);

        float t = (rhs.x * b2 - b1 * rhs.y) / det;

        Vector2 intersection = new Vector2(line1[2], line1[3]) + t * new Vector2(line1[0], line1[1]);
        Debug.Log(intersection);
        return intersection;
    }

    void DrawLine(Vector2 start, Vector2 end) {
        Color color = new Color(0.3f, 0.5f, 1.0f);
        Debug.DrawLine(start, end, color);
    }
}
