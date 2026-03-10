using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GameArchitecture;
using Utilities.Splines;
using WirePoleSystem;
using GUID = Utilities.GUID;

namespace Procedural
{
    [ExecuteInEditMode]
    public class ProceduralManager : MonoBehaviour
    {
        public MeshCollection TargetMeshCollection;
        
        // GUID Lookup
        public readonly Dictionary<GUID, BezierKnot> Knots = new();                  // Knot guid, Knot reference
        public readonly Dictionary<GUID, Road> Roads = new();                        // Road guid, road reference
        public readonly Dictionary<GUID, Intersection> Intersections = new();        // Intersection guid, intersection reference
        public readonly Dictionary<GUID, Sidewalk> Sidewalks = new();                // Sidewalk guid, sidewalk reference
        public readonly Dictionary<GUID, BlockArea> BlockAreas = new();              // BlockArea guid, blockArea reference
        public readonly Dictionary<GUID, Building> Buildings = new();                // Building guid, building reference
        public readonly Dictionary<GUID, Decoration> Decorations = new();            // Decoration guid, decoration
        public readonly Dictionary<GUID, WireConnector> WireConnectors = new();      // WireConnector guid, WireConnector reference
        public readonly Dictionary<GUID, ConnectionPoint> ConnectionPoints = new();  // ConnectionPoint guid, ConnectionPoint reference
        
        // Relations
        public readonly Dictionary<GUID, List<GUID>> RoadSidewalks = new();          // Road guid, list of attached sidewalk guids
        public readonly Dictionary<GUID, List<GUID>> KnotIntersections = new();      // Knot guid, list of attached intersection guids
        public readonly Dictionary<GUID, List<GUID>> SidewalkAreas = new();          // Sidewalk guid, list of attached areas
        public readonly Dictionary<GUID, List<GUID>> ObjectDecorations = new();      // BlockArea/Building Guid, Decorative Guid
        public readonly Dictionary<GUID, List<GUID>> Wires = new();                  // ConnectionPoint Guid, ConnectionPoint Guids
       
        // Prefabs
        [SerializeField] private GameObject RoadPrefab;
        [SerializeField] private GameObject IntersectionPrefab;
        [SerializeField] private GameObject SidewalkPrefab;
        [SerializeField] private GameObject BlockAreaPrefab;
        [SerializeField] private GameObject BuildingPrefab;

        public DecorationCollection DecorationCollection;

        [System.NonSerialized]
        private bool m_Initialized;

        public bool Initialized => m_Initialized;

        public void Register()
        {
            ReferenceManager.AddReference_Self(gameObject, this);
        }

        public void Setup() { }

        public void Selected()
        {
            Initialize();
        }

        public void Deselected() { }
        
        public void Initialize()
        {
            Roads.Clear();
            Intersections.Clear();
            Knots.Clear();
            Sidewalks.Clear();
            BlockAreas.Clear();
            Buildings.Clear();
            Decorations.Clear();
            WireConnectors.Clear();
            ConnectionPoints.Clear();
            
            RoadSidewalks.Clear();
            KnotIntersections.Clear();
            SidewalkAreas.Clear();
            ObjectDecorations.Clear();
            Wires.Clear();

            Road[] roads = GetComponentsInChildren<Road>(true);
            foreach (Road road in roads)
            {
                Roads.Add(road.Guid, road);

                foreach (BezierKnot knot in road.Spline)
                {
                    if (knot.Guid.Key == "") continue;

                    Knots.Add(knot.Guid, knot);
                }
            }

            Intersection[] intersections = GetComponentsInChildren<Intersection>(true);
            foreach (Intersection intersection in intersections)
            {
                Intersections.Add(intersection.Guid, intersection);

                // Add intersetion to knots
                foreach (RoadAttachment attachment in intersection.Attachments)
                {
                    if (!KnotIntersections.ContainsKey(attachment.KnotGuid))
                    {
                        KnotIntersections.Add(attachment.KnotGuid, new List<GUID>());
                    }

                    KnotIntersections[attachment.KnotGuid].Add(intersection.Guid);
                }
            }

            Sidewalk[] sidewalks = GetComponentsInChildren<Sidewalk>(true);
            foreach (Sidewalk sidewalk in sidewalks)
            {
                Sidewalks.Add(sidewalk.Guid, sidewalk);

                if (!RoadSidewalks.ContainsKey(sidewalk.TargetGuid))
                {
                    RoadSidewalks.Add(sidewalk.TargetGuid, new List<GUID>());
                }

                if (RoadSidewalks.TryGetValue(sidewalk.TargetGuid, out List<GUID> roadSidewalks))
                {
                    roadSidewalks.Add(sidewalk.Guid);
                }
            }

            BlockArea[] areas = GetComponentsInChildren<BlockArea>(true);
            foreach (BlockArea area in areas)
            {
                BlockAreas.Add(area.Guid, area);

                foreach (SidewalkHandle element in area.Guids)
                {
                    if (!SidewalkAreas.ContainsKey(element.Guid))
                    {
                        SidewalkAreas.Add(element.Guid, new List<GUID>());
                    }

                    if (SidewalkAreas.TryGetValue(element.Guid, out List<GUID> sidewalkAreas))
                    {
                        sidewalkAreas.Add(area.Guid);
                    }
                }

                area.Initialize();
            }

            Building[] buildings = GetComponentsInChildren<Building>(true);
            foreach (Building building in buildings)
            {
                Buildings.Add(building.Guid, building);
            }

            WirePole[] wirePoles = GetComponentsInChildren<WirePole>(true);
            foreach (WirePole wirePole in wirePoles)
            {
                foreach (ConnectionPoint connectionPoint in wirePole.ConnectionPoints) ConnectionPoints.Add(connectionPoint.Guid, connectionPoint);
            }
            
            InitializeDecorations();
            InitializeWirePoles();

            m_Initialized = true;
        }

        private void InitializeDecorations()
        {
            Decoration[] decorations = GetComponentsInChildren<Decoration>(true);
            foreach (Decoration decoration in decorations)
            {
                if (decoration.Procedural || decoration.Guid == GUID.None || !Decorations.TryAdd(decoration.Guid, decoration)) continue;
                
                // Create a new list for the parent GUID if it doesn't exist yet and add the decoration GUID to it
                List<GUID> decorationList;
                
                if (!ObjectDecorations.TryGetValue(decoration.ParentGuid, out List<GUID> objectDecorations))
                {
                    decorationList = new List<GUID>();
                    ObjectDecorations.Add(decoration.ParentGuid, decorationList);
                }
                else decorationList = objectDecorations;
                
                decorationList.Add(decoration.Guid);
            }
        }

        private void InitializeWirePoles()
        {
            WireConnector[] wireConnectors = GetComponentsInChildren<WireConnector>(true);
            foreach (WireConnector wireConnector in wireConnectors)
            {
                WireConnectors.Add(wireConnector.Guid, wireConnector);
                
                foreach (ConnectionPoint connectionPoint in wireConnector.ConnectionPoints)
                {
                    ConnectionPoints.TryAdd(connectionPoint.Guid, connectionPoint);
                }
            }
        }

        private void InitializeWires()
        {
            WireGenerator[] wires = GetComponentsInChildren<WireGenerator>();
            foreach (WireGenerator wire in wires)
            {
                if (Wires.TryGetValue(wire.StartGuid, out List<GUID> endPoints))
                    if (!endPoints.Contains(wire.EndGuid)) endPoints.Add(wire.EndGuid);
                else if (Wires.TryGetValue(wire.EndGuid, out List<GUID> startPoints))
                    if (!startPoints.Contains(wire.StartGuid)) startPoints.Add(wire.StartGuid);
                else Wires.Add(wire.StartGuid, new List<GUID>{ wire.EndGuid });
            }
        }

        private ProceduralMesh GetProceduralMeshByGuid(GUID Guid)
        {
            return (ProceduralMesh) Roads.GetValueOrDefault(Guid) ??
                   (ProceduralMesh) Intersections.GetValueOrDefault(Guid) ??
                   (ProceduralMesh) Sidewalks.GetValueOrDefault(Guid) ??
                   (ProceduralMesh) BlockAreas.GetValueOrDefault(Guid) ??
                   (ProceduralMesh) Buildings.GetValueOrDefault(Guid);
        }

        // Road functions
        public bool AddRoad(out GameObject roadObject)
        {
            if (!RoadPrefab || !RoadPrefab.GetComponent<Road>())
            {
                Debug.LogWarning("Can not create road without assigned prefab");
                roadObject = null;
                return false;
            }

            roadObject = (GameObject)InstantiateProceduralPrefab(RoadPrefab, transform);
            roadObject.transform.position = Vector3.zero;
            roadObject.transform.rotation = Quaternion.identity;

            if (!roadObject.TryGetComponent(out Road road)) return false;
            road.Construct();
            roadObject.name = $"Road {road.Guid}";
            RegisterObject(roadObject, roadObject.name);
            Roads.Add(road.Guid, road);

            // Add sidewalk
            AddRoadSidewalk(road.Guid, SidewalkType.RoadRight, out _);
            AddRoadSidewalk(road.Guid, SidewalkType.RoadLeft, out _);

            return true;
        }

        public void RoadRemoved(GUID roadGuid)
        {
            if (Application.isPlaying) return;

            if (!Roads.TryGetValue(roadGuid, out Road road)) return;

            Roads.Remove(roadGuid);

            //todo remove road from intersections

            if (RoadSidewalks.ContainsKey(roadGuid))
            {
                // Sidewalks should already be destroyed by the road editor
                RoadSidewalks.Remove(roadGuid);
            }

            foreach (BezierKnot knot in road.Spline) Knots.Remove(knot.Guid);
        }

        public void RegenerateRoadAttachments(GUID targetRoadGuid)
        {
            // Update intersections
            if (!Roads.TryGetValue(targetRoadGuid, out Road road)) return;

            foreach (BezierKnot knot in road.Spline)
            {
                if (!KnotIntersections.TryGetValue(knot.Guid, out List<GUID> intersectionGuids)) continue;

                foreach (GUID intersectionGuid in intersectionGuids)
                {
                    Intersections[intersectionGuid].Generate();
                }
            }

            // Update sidewalks
            GenerateRoadSidewalks(targetRoadGuid);
        }

        private void GenerateRoadSidewalks(GUID targetRoadGuid)
        {
            if (!RoadSidewalks.TryGetValue(targetRoadGuid, out List<GUID> roadSidewalk)) return;

            foreach (GUID sidewalkGuid in roadSidewalk)
            {
                if (!Sidewalks.TryGetValue(sidewalkGuid, out Sidewalk sidewalk)) continue;
                sidewalk.Generate(false);

                if (!SidewalkAreas.TryGetValue(sidewalkGuid, out List<GUID> sidewalkAreas)) continue;
                foreach(GUID areaGuid in sidewalkAreas) BlockAreas[areaGuid].Generate();
            }
        }

        // Intersection functions
        public bool AddIntersection(List<RoadAttachment> attachments, out GameObject intersectionObject)
        {
            if (!IntersectionPrefab || !IntersectionPrefab.GetComponent<Intersection>())
            {
                Debug.LogWarning("Can not create intersection without assigned prefab");
                intersectionObject = null;
                return false;
            }

            intersectionObject = (GameObject)InstantiateProceduralPrefab(IntersectionPrefab, transform);
            intersectionObject.transform.position = Vector3.zero;
            intersectionObject.transform.rotation = Quaternion.identity;

            if (!intersectionObject.TryGetComponent(out Intersection intersection)) return false;
            intersection.Construct(attachments);
            intersectionObject.name = $"Intersection {intersection.Guid}";
            RegisterObject(intersectionObject, intersectionObject.name);
            Intersections.Add(intersection.Guid, intersection);

            foreach (RoadAttachment attachment in attachments)
            {
                if (!Knots.TryGetValue(attachment.KnotGuid, out BezierKnot knot)) continue;

                // Add intersection to knots
                if (!KnotIntersections.ContainsKey(attachment.KnotGuid))
                {
                    KnotIntersections.Add(attachment.KnotGuid, new List<GUID>());
                }

                KnotIntersections[attachment.KnotGuid].Add(intersection.Guid);
            }

            // Add sidewalks
            AddIntersectionSidewalks(intersection.Guid, attachments);

            return true;

        }

        public void RegenerateIntersectionAttachments(GUID intersectionGuid)
        {
            // Update sidewalks
            if (!RoadSidewalks.TryGetValue(intersectionGuid, out List<GUID> roadSidewalk)) return;

            foreach (GUID sidewalkGuid in roadSidewalk)
            {
                if (Sidewalks.TryGetValue(sidewalkGuid, out Sidewalk sidewalk)) sidewalk.Generate();
            }
        }

        public void IntersectionRemoved(GUID intersectionGuid)
        {
            if (Application.isPlaying) return;

            if (!Intersections.TryGetValue(intersectionGuid, out Intersection intersection)) return;

            foreach (RoadAttachment attachment in intersection.Attachments)
            {
                if (!RoadSidewalks.TryGetValue(attachment.RoadGuid, out List<GUID> roadSidewalks)) continue;

                // Remove sidewalk break from roads
                foreach (GUID sidewalkGuid in roadSidewalks)
                {
                    if (!Sidewalks.TryGetValue(sidewalkGuid, out Sidewalk sidewalk)) continue;

                    for (int i = sidewalk.Breaks.Count - 1; i >= 0; i--)
                    {
                        if (sidewalk.Breaks[i].Reason != intersectionGuid) continue;
                        
                        sidewalk.RemoveBreak(i);
                        sidewalk.Generate(false);
                    }
                }

                // Remove knot
                if (!Knots.TryGetValue(attachment.KnotGuid, out BezierKnot knot)) continue;
                if (!KnotIntersections.TryGetValue(knot.Guid, out List<GUID> intersectionGuids)) continue;

                intersectionGuids.Remove(intersectionGuid);
            }

            Intersections.Remove(intersectionGuid);
        }

        public void RoadAttachmentChanged(GUID roadGuid)
        {
            if (!Roads.TryGetValue(roadGuid, out Road road)) return;

            road.Generate(false);

            // Regenerate road sidewalks
            GenerateRoadSidewalks(roadGuid);
        }
        
        public void GenerateIntersections(GUID knotGuid)
        {
            if (!KnotIntersections.TryGetValue(knotGuid, out List<GUID> intersections)) return;

            foreach (GUID intersectionGuid in intersections)
            {
                if (!Intersections.TryGetValue(intersectionGuid, out Intersection intersection)) continue;

                intersection.Generate();
            }
        }

        // Sidewalk functions
        public bool AddRoadSidewalk(GUID targetGuid, SidewalkType type, out GameObject sidewalkObject)
        {
            if (!SidewalkPrefab || !SidewalkPrefab.GetComponent<Sidewalk>() || type == SidewalkType.Connection)
            {
                Debug.LogWarning("Could not create sidewalk");
                sidewalkObject = null;
                return false;
            }

            sidewalkObject = (GameObject)InstantiateProceduralPrefab(SidewalkPrefab, transform);
            sidewalkObject.transform.position = Vector3.zero;
            sidewalkObject.transform.rotation = Quaternion.identity;

            if (sidewalkObject.TryGetComponent(out Sidewalk sidewalk))
            {
                sidewalk.ConstructForRoad(targetGuid, type);
                sidewalkObject.name = $"Sidewalk {sidewalk.Guid}";
                RegisterObject(sidewalkObject, sidewalkObject.name);
                Sidewalks.Add(sidewalk.Guid, sidewalk);

                if (!RoadSidewalks.ContainsKey(sidewalk.TargetGuid))
                {
                    RoadSidewalks.Add(sidewalk.TargetGuid, new List<GUID>());
                }

                if (RoadSidewalks.TryGetValue(sidewalk.TargetGuid, out List<GUID> roadSidewalks))
                {
                    roadSidewalks.Add(sidewalk.Guid);
                }

                return true;
            }

            Debug.LogWarning("Error creating sidewalk - No sidewalk script attached to prefab");
            return false;
        }

        private bool AddIntersectionSidewalks(GUID intersectionGuid, List<RoadAttachment> attachments)
        {
            if (!SidewalkPrefab)
            {
                Debug.LogWarning("Can not create sidewalk without assigned prefab");
                return false;
            }

            Vector3 center = GetCenter(attachments);

            attachments.Sort((a, b) =>
            {
                if (!Knots.TryGetValue(a.KnotGuid, out BezierKnot knotA)) return 0;
                if (!Knots.TryGetValue(b.KnotGuid, out BezierKnot knotB)) return 0;

                float angleA = Mathf.Atan2(knotA.Position.z - center.z, knotA.Position.x - center.x);
                float angleB = Mathf.Atan2(knotB.Position.z - center.z, knotB.Position.x - center.x);
                return angleA.CompareTo(angleB);
            });

            for (int i = 0; i < attachments.Count; i++)
            {
                RoadAttachment attachment1 = attachments[i];
                RoadAttachment attachment2 = attachments[(i + 1) % attachments.Count];

                GameObject sidewalkObject = (GameObject)InstantiateProceduralPrefab(SidewalkPrefab, transform);
                sidewalkObject.transform.position = Vector3.zero;
                sidewalkObject.transform.rotation = Quaternion.identity;

                if (sidewalkObject.TryGetComponent(out Sidewalk sidewalk))
                {
                    sidewalk.ConstructForIntersection(intersectionGuid, attachment1, attachment2);

                    sidewalkObject.name = $"Sidewalk {sidewalk.Guid}";
                    RegisterObject(sidewalkObject, sidewalkObject.name);
                    Sidewalks.Add(sidewalk.Guid, sidewalk);

                    if (!RoadSidewalks.ContainsKey(sidewalk.TargetGuid))
                    {
                        RoadSidewalks.Add(sidewalk.TargetGuid, new List<GUID>());
                    }

                    if (RoadSidewalks.TryGetValue(sidewalk.TargetGuid, out List<GUID> roadSidewalks))
                    {
                        roadSidewalks.Add(sidewalk.Guid);
                    }
                }
                else
                {
                    Debug.LogWarning("Error creating sidewalk - No sidewalk script attached to prefab");
                    return false;
                }
            }

            return attachments.Count > 1;
        }

        public bool AddBlockArea(List<SidewalkHandle> elements, out GameObject blockAreaObject)
        {
            if (!BlockAreaPrefab)
            {
                Debug.LogWarning("Can not create block area without assigned prefab");
                blockAreaObject = null;
                return false;
            }

            blockAreaObject = (GameObject)InstantiateProceduralPrefab(BlockAreaPrefab, transform);
            blockAreaObject.transform.position = Vector3.zero;
            blockAreaObject.transform.rotation = Quaternion.identity;

            if (blockAreaObject.TryGetComponent(out BlockArea blockArea))
            {
                blockArea.Construct(elements);

                blockArea.name = $"BlockArea {blockArea.Guid}";
                RegisterObject(blockAreaObject, blockAreaObject.name);
                BlockAreas.Add(blockArea.Guid, blockArea);

                foreach (SidewalkHandle element in elements)
                {
                    if (!SidewalkAreas.ContainsKey(element.Guid))
                    {
                        SidewalkAreas.Add(element.Guid, new List<GUID>());
                    }

                    if (SidewalkAreas.TryGetValue(element.Guid, out List<GUID> sidewalkAreas))
                    {
                        sidewalkAreas.Add(blockArea.Guid);
                    }
                }

                return true;
            }

            Debug.LogWarning("Error creating block area - No blockArea script attached to prefab");
            return false;
        }

        public bool UpdateBlockArea(GUID blockAreaGuid, List<SidewalkHandle> elements, out GameObject blockAreaObject)
        {
            if (BlockAreas.TryGetValue(blockAreaGuid, out BlockArea blockArea))
            {
                blockAreaObject = blockArea.gameObject;
                blockArea.Guids = elements;
                blockArea.Generate();

                foreach (SidewalkHandle element in elements)
                {
                    if (!SidewalkAreas.ContainsKey(element.Guid))
                    {
                        SidewalkAreas.Add(element.Guid, new List<GUID>());
                    }

                    if (SidewalkAreas.TryGetValue(element.Guid, out List<GUID> sidewalkAreas))
                    {
                        sidewalkAreas.Add(blockArea.Guid);
                    }
                }

                return true;
            }

            Debug.LogWarning("Error creating block area - No blockArea script attached to prefab");
            blockAreaObject = null;
            return false;
        }

        public bool AddBuilding(BlockArea blockArea, out GameObject buildingObject)
        {
            if (!BuildingPrefab)
            {
                Debug.LogWarning("Can not create building without assigned prefab");
                buildingObject = null;
                return false;
            }

            buildingObject = (GameObject)InstantiateProceduralPrefab(BuildingPrefab, blockArea.gameObject.transform);
            buildingObject.transform.position = Vector3.zero;
            buildingObject.transform.rotation = Quaternion.identity;

            if (buildingObject.TryGetComponent(out Building building))
            {
                building.Construct();

                building.name = $"Building {building.Guid}";
                RegisterObject(buildingObject, buildingObject.name);
                Buildings.Add(building.Guid, building);
                blockArea.Buildings.Add(building.Guid, building);

                return true;
            }

            Debug.LogWarning("Error creating building - No building script attached to prefab");
            return false;
        }

        private GameObject PlaceProceduralAsset(Vector3 position, Quaternion rotation, ProceduralAsset proceduralAsset)
        {
            GameObject assetObject = proceduralAsset.Prefab;
            
            if (assetObject is null)
            {
                Debug.LogError($"{proceduralAsset} does not have a prefab assigned.");
                return null;
            }
            
            GameObject instantiatedObject = InstantiateProceduralPrefab(assetObject, transform, false);
            
            Transform objectTransform = instantiatedObject.transform;
            objectTransform.position = position;
            objectTransform.rotation = rotation;
            
            instantiatedObject.isStatic = true;
            
            return instantiatedObject;
        }
        
        // Decoration functions
        public GUID AddDecoration(Vector3 position, Quaternion rotationOffset, ProceduralAsset decorationAsset)
        {
            GameObject decorationObject = PlaceProceduralAsset(position, rotationOffset, decorationAsset);
            Decoration decoration = decorationObject.GetComponent<Decoration>();
            
            if (!decoration)
            {
                Debug.LogError($"No decoration component found on {decorationObject.name}.");
                DestroyImmediate(decorationObject);
                return GUID.None;
            }
            RegisterObject(decorationObject, "Placed Decoration");

            decoration.Procedural = false;
            decoration.Guid = GUID.Create();
            Decorations[decoration.Guid] = decoration;
            
            return decoration.Guid;
        }

        public GUID AddDecoration(Vector3 position, Quaternion rotationOffset, ProceduralAsset decorationAsset, GUID parentGuid)
        {
            GUID decorationGuid = AddDecoration(position, rotationOffset, decorationAsset);
            if (!Decorations.TryGetValue(decorationGuid, out Decoration decoration)) return decorationGuid;
            GameObject decorationObject = decoration.gameObject;
            
            GameObject parentObject = GetProceduralMeshByGuid(parentGuid).gameObject;
            decorationObject.transform.parent = parentObject.transform;
            (ObjectDecorations[parentGuid] ??= new List<GUID>()).Add(decorationGuid);

            return decorationGuid;
        }

        public GUID AddWirePole(Vector3 position, Quaternion rotationOffset, ProceduralAsset wirePoleAsset)
        {
            GameObject wirePoleObject = PlaceProceduralAsset(position, rotationOffset, wirePoleAsset);
            RegisterObject(wirePoleObject, "Create Wirepole");
            WirePole wirePole = wirePoleObject.GetComponent<WirePole>();
            wirePole.Guid = GUID.Create();
            WireConnectors.Add(wirePole.Guid, wirePole);
            return wirePole.Guid;
        }

        public void ConnectWire(ConnectionPoint startPoint, ConnectionPoint endPoint)
        {
            if (CheckWireConnection(startPoint.Guid, endPoint.Guid)) return;
            WireConnector startConnector = startPoint.Parent;
            WireConnector endConnector = endPoint.Parent;
            GameObject newWire = startConnector.Connect(startPoint, endPoint).gameObject;
            AddWireConnection(startPoint.Guid, endPoint.Guid);
            
            endConnector.OnTransformChange += startConnector.UpdateWires;
            
            RegisterObject(newWire, "Connected Wire");
        }

        public void ConnectWires(ConnectionPoint startPoint, ConnectionPoint[] endPoints)
        {
            foreach (ConnectionPoint endPoint in endPoints) ConnectWire(startPoint, endPoint);
        }

        private bool CheckWireConnection(GUID startGuid, GUID endGuid)
        {
            if (Wires.TryGetValue(startGuid, out List<GUID> endGuids) && endGuids.Contains(endGuid)) return true;
            if (Wires.TryGetValue(endGuid, out List<GUID> startGuids) && startGuids.Contains(startGuid)) return true;
            return false;
        }

        private void AddWireConnection(GUID startGuid, GUID endGuid)
        {
            if (Wires.TryGetValue(startGuid, out List<GUID> endGuids)) endGuids.Add(endGuid);
            else if (Wires.TryGetValue(endGuid, out List<GUID> startGuids)) startGuids.Add(startGuid);
            else Wires.Add(startGuid, new List<GUID>{endGuid});
        }
        
        // Utility functions
        private Vector3 GetCenter(List<RoadAttachment> attachments)
        {
            Vector3 center = attachments.Aggregate(Vector3.zero, (current, attachment) => current + Knots[attachment.KnotGuid].Position);

            return center / attachments.Count;
        }

        public void SidewalkRemoved(GUID guid)
        {
            if (Application.isPlaying) return;

            Sidewalks.Remove(guid);

            // Remove sidewalk reference from area
            if (!SidewalkAreas.TryGetValue(guid, out List<GUID> areaGuids)) return;

            foreach (GUID areaGuid in areaGuids)
            {
                if (!BlockAreas.TryGetValue(areaGuid, out BlockArea area)) continue;

                List<SidewalkHandle> handles = new List<SidewalkHandle>(area.Guids);
                
                for (int i = handles.Count - 1; i >= 0; i--)
                {
                    if (handles[i].Guid != guid) continue;

                    handles.RemoveAt(i);
                }

                Undo.RecordObject(area, "Remove Sidewalk From Area");
                area.Construct(handles);
            }
        }

        public void BlockAreaRemoved(GUID guid)
        {
            if (Application.isPlaying) return;

            BlockAreas.Remove(guid);
        }

        public void RegenerateSidewalksAttachments(GUID targetSidewalkGuid)
        {
            if (!Sidewalks.TryGetValue(targetSidewalkGuid, out Sidewalk sidewalk)) return;

            if (sidewalk.Type == SidewalkType.Connection)
            {
                // Regenerate attached areas
                if (!SidewalkAreas.TryGetValue(targetSidewalkGuid, out List<GUID> areaGuids)) return;

                foreach (GUID areaGuid in areaGuids)
                {
                    if (!BlockAreas.TryGetValue(areaGuid, out BlockArea area)) continue;

                    area.Generate();
                }
            }
            else
            {
                if (!Roads.TryGetValue(sidewalk.TargetGuid, out Road road)) return;

                foreach (BezierKnot knot in road.Spline)
                {
                    if (!KnotIntersections.TryGetValue(knot.Guid, out List<GUID> intersection)) continue;

                    foreach (GUID sidewalkGuid in intersection.SelectMany(intersectionGuid => RoadSidewalks[intersectionGuid]))
                    {
                        if (!Sidewalks.TryGetValue(sidewalkGuid, out Sidewalk otherSidewalk)) continue;
                        otherSidewalk.Generate();
                    }
                }
            }
        }

        public static GameObject InstantiateProceduralPrefab(Object assetComponentOrGameObject, Transform parent, bool addTag = true)
        {
            
#if UNITY_EDITOR
            GameObject prefab = (GameObject)PrefabUtility.InstantiatePrefab(assetComponentOrGameObject, parent);
            EditorUtility.SetDirty(prefab);
#else
            GameObject prefab = (GameObject)Object.Instantiate(assetComponentOrGameObject, parent);
#endif
            if(addTag) prefab.tag = "Procedural";
            return prefab;
        }

        public static void RegisterObject(Object objectToUndo, string name)
        {
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(objectToUndo, name);
#endif
        }

        [ContextMenu("Regenerate")]
        public void Regenerate()
        {
            foreach (Road road in Roads.Values)
            {
                if (!road) continue; //Null conditional operator does not work on Monobehaviors

                road.Generate(false);
            }

            foreach (Intersection intersection in Intersections.Values)
            {
                if (!intersection) continue;

                intersection.Generate(false);
            }

            foreach (Sidewalk sidewalk in Sidewalks.Values)
            {
                if (!sidewalk) continue;

                sidewalk.Generate(false);
            }

            foreach (BlockArea area in BlockAreas.Values)
            {
                if (!area) continue;

                area.Generate(false);
            }

            foreach (Building building in Buildings.Values)
            {
                if (!building) continue;

                building.Generate(false);
            }

            MeshUtils.SaveMesh();
        }


        [ContextMenu("Validate")]
        public void Validate()
        {
            ValidateRoads();
            ValidateIntersections();
        }

        public void ValidateRoads()
        {
            foreach (KeyValuePair<GUID, Road> road in Roads)
            {
                foreach (BezierKnot knot in road.Value.Spline)
                {
                    if (!KnotIntersections.TryGetValue(knot.Guid, out List<GUID> intersectionGuids)) continue;

                    for (int i = intersectionGuids.Count - 1; i >= 0; i--)
                    {
                        if (!Intersections.ContainsKey(intersectionGuids[i]))
                        {
                            KnotIntersections[knot.Guid].RemoveAt(i);
                        }
                    }
                }
            }
        }

        public void ValidateIntersections()
        {

            foreach (KeyValuePair<GUID, Intersection> intersection in Intersections)
            {

            }
        }
    }
}

// Save system
// Get the position on the splins based on t (0 = start of spline, 1 = end of spline)
//public Vector3 Evaluate(float t)
//{
//    if (Knots == null || Knots.Count == 0) return Vector3.zero;
//    if (Knots.Count == 1) return Knots[0].Position;
//}

// private readonly static string m_SavePath = Application.persistentDataPath + "/Data/Procedural/";
//
// public static void Save<T>(T data, string fileName)
// {
//     string json = JsonUtility.ToJson(data, true);
//     File.WriteAllText(Path.Combine(m_SavePath, fileName + ".json"), json);
//     Debug.Log($"Saved {fileName} to {m_SavePath}");
// }
//
// public static T Load<T>(string fileName)
// {
//     string fullPath = Path.Combine(m_SavePath, fileName + ".json");
//     if (!File.Exists(fullPath))
//     {
//         Debug.LogWarning($"File {fullPath} not found.");
//         return default;
//     }
//
//     string json = File.ReadAllText(fullPath);
//     return JsonUtility.FromJson<T>(json);
// }