using UnityEngine;
using System.Collections.Generic;

public class RoamZoneManager : MonoBehaviour
{
    public static RoamZoneManager Instance { get; private set; }
    private List<RoamZone> roamZones = new List<RoamZone>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        roamZones.AddRange(FindObjectsByType<RoamZone>(FindObjectsSortMode.None));
    }

    public List<RoamZone> GetAllZones()
    {
        return roamZones;
    }    
}
