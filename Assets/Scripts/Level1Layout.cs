using UnityEngine;

/// <summary>
/// Optional helper for Level 1 - Follow Your Ghost. Holds references to the two plates and the door
/// so you have one place to see the level's key objects. Does not wire the Door for you: on the Door
/// component, set Plates size to 2 and assign Plate A and Plate B; keep Require All Plates checked.
/// </summary>
public class Level1Layout : MonoBehaviour
{
    [Header("Level 1 – Follow Your Ghost")]
    [Tooltip("First plate (player stands here, then rewinds so ghost holds it).")]
    [SerializeField] private PressurePlate plateA;
    [Tooltip("Second plate (player runs here after rewind; door opens when both A and B are pressed).")]
    [SerializeField] private PressurePlate plateB;
    [Tooltip("Door that opens when BOTH plates are pressed. Assign plateA and plateB on the Door component.")]
    [SerializeField] private Door door;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (door == null) return;
        int count = 0;
        try
        {
            var so = new UnityEditor.SerializedObject(door);
            var platesProp = so.FindProperty("plates");
            if (platesProp != null) count = platesProp.arraySize;
        }
        catch { /* ignore */ }
        if (count != 2 && (plateA != null || plateB != null))
            Debug.LogWarning("Level1Layout: On the Door, set Plates size to 2 and assign Plate A and Plate B. Require All Plates = true.", door);
    }
#endif
}
