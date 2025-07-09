using System;
using StixGames.NatureCore;
using StixGames.NatureCore.Utility;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StixGames.GrassShader
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(NatureMeshFilter))]
    [AddComponentMenu("Stix Games/General/Grass Renderer", 1)]
    public class GrassRenderer : MonoBehaviour
    {
        public Material Material;

        [Tooltip("If the mesh has multiple materials, this can be used to select the target material index.")]
        public int MaterialIndex = 0;

        [LayerField] public int Layer;
        public ShadowCastingMode ShadowCastingMode = ShadowCastingMode.On;
        public bool ReceiveShadows = true;

        [Space] public bool AutoSetFloorColor = false;

        [Tooltip("If true, the renderer will not render meshes that are not visible by the main camera.")]
        public bool UseMainCameraFrustumCulling = true;

        private NatureMeshFilter _natureMeshFilter;

        private MaterialPropertyBlock propertyBlock = null;

        private Shader currentGrassShader = null;

        private Plane[] planes = new Plane[6];

        private static bool hasInitializedGlobalVariables;

        private static readonly int GrassTime = Shader.PropertyToID("_GrassTime");

        private static readonly int GlobalTargetDensityMultiplier =
            Shader.PropertyToID("_GlobalTargetDensityMultiplier");

        // Reusable array for bounds transformation to avoid allocations
        private readonly Vector3[] corners = new Vector3[8];

        public NatureMeshFilter NatureMeshFilter
        {
            get
            {
                return _natureMeshFilter != null
                    ? _natureMeshFilter
                    : (_natureMeshFilter = GetComponent<NatureMeshFilter>());
            }
        }

        private void Awake()
        {
            SetupGlobalVariables();
        }

        private void SetupGlobalVariables()
        {
            if (hasInitializedGlobalVariables)
            {
                return;
            }

            hasInitializedGlobalVariables = true;

            if (Shader.GetGlobalFloat(GlobalTargetDensityMultiplier) == 0.0f)
            {
                Shader.SetGlobalFloat(GlobalTargetDensityMultiplier, 1);
            }
        }

        private void Start()
        {
            if (AutoSetFloorColor)
            {
                var renderer = GetComponent<Renderer>();
                var terrain = GetComponent<Terrain>();
                UpdateFloorColor(renderer, terrain);
            }
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
#if UNITY_2019_2_OR_NEWER
            SceneView.duringSceneGui -= EditorGrassTimeUpdate;
            SceneView.duringSceneGui += EditorGrassTimeUpdate;
#else
            SceneView.onSceneGUIDelegate -= EditorGrassTimeUpdate;
            SceneView.onSceneGUIDelegate += EditorGrassTimeUpdate;
#endif
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
#if UNITY_2019_2_OR_NEWER
            SceneView.duringSceneGui -= EditorGrassTimeUpdate;
#else
            SceneView.onSceneGUIDelegate -= EditorGrassTimeUpdate;
#endif
#endif
        }

#if UNITY_EDITOR
        public void EditorGrassTimeUpdate(SceneView view)
        {
            UpdateGrassTime();
            SceneView.RepaintAll();
#if UNITY_2019_2_OR_NEWER
            EditorApplication.QueuePlayerLoopUpdate();
#endif
        }
#endif

        /// <summary>
        /// Set a material property block that will be used for rendering.
        /// </summary>
        /// <param name="propertyBlock"></param>
        public void SetPropertyBlock(MaterialPropertyBlock propertyBlock)
        {
            this.propertyBlock = propertyBlock;
        }

        private void LateUpdate()
        {
            if (Material == null)
            {
                return;
            }

            if (currentGrassShader != Material.shader)
            {
                if (!GrassUtility.IsGrassMaterial(Material))
                {
                    throw new InvalidOperationException("GrassMaterial does not use the DX11 Grass Shader");
                }

                currentGrassShader = Material.shader;
            }

#if UNITY_EDITOR
            SetupGlobalVariables();
#endif

            var renderer = GetComponent<Renderer>();
            var terrain = GetComponent<Terrain>();

            if (Application.isEditor && !Application.isPlaying && AutoSetFloorColor)
            {
                UpdateFloorColor(renderer, terrain);
            }

            if (Material.shader.isSupported)
            {
                UpdateGrassTime();

                var maxGrassHeight = GrassUtility.GetMaxGrassHeight(Material);
                Vector3 localBoundingBoxExpansion;
                if (renderer != null)
                {
                    // The mesh (and bounding box) will be scaled according to the local scale
                    localBoundingBoxExpansion = new Vector3(maxGrassHeight / transform.localScale.x,
                        maxGrassHeight / transform.localScale.y, maxGrassHeight / transform.localScale.z);
                }
                else
                {
                    // Terrain is never scaled
                    localBoundingBoxExpansion = new Vector3(maxGrassHeight, maxGrassHeight, maxGrassHeight);
                }

#if UNITY_EDITOR
                var mainCamera = Application.isPlaying ? Camera.main : SceneView.lastActiveSceneView?.camera;
#else
                var mainCamera = Camera.main;
#endif

                // Calculate a new frustum that
                if (UseMainCameraFrustumCulling && mainCamera != null)
                {
                    var farDistance = Mathf.Min(mainCamera.farClipPlane,
                        GrassUtility.GetGrassFadeEnd(Material) + maxGrassHeight);
                    var modifiedProjectionMatrix =
                        Matrix4x4.Perspective(mainCamera.fieldOfView, mainCamera.aspect, mainCamera.nearClipPlane,
                            farDistance);
                    var modifiedVP = modifiedProjectionMatrix * mainCamera.worldToCameraMatrix;
                    GeometryUtility.CalculateFrustumPlanes(modifiedVP, planes);
                }

                var meshes = NatureMeshFilter.GetMeshes();
                foreach (var mesh in meshes)
                {
                    // Update bounds with grass height
                    var original = mesh.bounds;
                    var modified = original;
                    modified.Expand(localBoundingBoxExpansion * 2);
                    mesh.bounds = modified;

                    try
                    {
                        var localToWorldMatrix = renderer != null
                            ? transform.localToWorldMatrix
                            : Matrix4x4.Translate(transform.position);

                        if (UseMainCameraFrustumCulling && mainCamera != null)
                        {
                            var meshWorldBounds = TransformBounds(localToWorldMatrix, modified);

                            // Additional frustum culling to only render meshes where the grass could be visible
                            if (!GeometryUtility.TestPlanesAABB(planes, meshWorldBounds))
                            {
                                continue;
                            }
                        }

                        // Debug visualization of bounding boxes in local space (temporary)
                        // DrawBounds(original, Color.blue, localToWorldMatrix);   // Original bounds in blue
                        // DrawBounds(modified, Color.green, localToWorldMatrix);  // Modified bounds in green
                        // DrawBounds(TransformBounds(localToWorldMatrix, modified), Color.red, Matrix4x4.identity);

                        // Draw mesh
                        Graphics.DrawMesh(mesh, localToWorldMatrix, Material, Layer, null, MaterialIndex, propertyBlock,
                            ShadowCastingMode, ReceiveShadows);
                    }
                    finally
                    {
                        // Restore bounds
                        mesh.bounds = original;
                    }
                }
            }
        }

        /// <summary>
        /// Transform a bounding box with a matrix. The new bounds completely encapsulate the transformed bounding box.
        /// Optimized for performance when called multiple times per frame using a reusable buffer.
        /// </summary>
        /// <param name="transformationMatrix"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        private Bounds TransformBounds(Matrix4x4 transformationMatrix, Bounds bounds)
        {
            // Fast path for identity matrix
            if (transformationMatrix.isIdentity)
            {
                return bounds;
            }
            
            var center = bounds.center;
            var extents = bounds.extents;
            
            // Inline corner calculations for better performance
            var minX = center.x - extents.x;
            var maxX = center.x + extents.x;
            var minY = center.y - extents.y;
            var maxY = center.y + extents.y;
            var minZ = center.z - extents.z;
            var maxZ = center.z + extents.z;
            
            corners[0] = new Vector3(minX, minY, minZ);
            corners[1] = new Vector3(maxX, minY, minZ);
            corners[2] = new Vector3(maxX, maxY, minZ);
            corners[3] = new Vector3(minX, maxY, minZ);
            corners[4] = new Vector3(minX, minY, maxZ);
            corners[5] = new Vector3(maxX, minY, maxZ);
            corners[6] = new Vector3(maxX, maxY, maxZ);
            corners[7] = new Vector3(minX, maxY, maxZ);

            // Transform first corner and initialize min/max
            corners[0] = transformationMatrix.MultiplyPoint3x4(corners[0]);
            var minResult = corners[0];
            var maxResult = corners[0];

            // Transform remaining corners and update min/max inline
            for (var i = 1; i < 8; i++)
            {
                corners[i] = transformationMatrix.MultiplyPoint3x4(corners[i]);
                
                if (corners[i].x < minResult.x)
                {
                    minResult.x = corners[i].x;
                }

                if (corners[i].y < minResult.y)
                {
                    minResult.y = corners[i].y;
                }

                if (corners[i].z < minResult.z)
                {
                    minResult.z = corners[i].z;
                }

                if (corners[i].x > maxResult.x)
                {
                    maxResult.x = corners[i].x;
                }

                if (corners[i].y > maxResult.y)
                {
                    maxResult.y = corners[i].y;
                }

                if (corners[i].z > maxResult.z)
                {
                    maxResult.z = corners[i].z;
                }
            }

            // Create new bounds from min/max
            var newCenter = (minResult + maxResult) * 0.5f;
            var newSize = maxResult - minResult;
            
            return new Bounds(newCenter, newSize);
        }

        private static void UpdateGrassTime()
        {
            Shader.SetGlobalVector(GrassTime,
                new Vector4(Time.time / 20, Time.time, Time.time * 2, Time.time * 3));
        }

        private void UpdateFloorColor(Renderer renderer, Terrain terrain)
        {
            if (renderer != null)
            {
                var floorMaterial = renderer.sharedMaterial;

                var baseColor = floorMaterial.color;
                var colorTexture = floorMaterial.mainTexture;
                var offset = floorMaterial.mainTextureOffset;
                var scale = floorMaterial.mainTextureScale;

                GrassUtility.SetFloorColor(Material, baseColor);
                GrassUtility.SetFloorColorTexture(Material, colorTexture, offset, scale);
                return;
            }

            if (terrain != null)
            {
                //TODO: Add terrain support for this

                Debug.LogWarning("Auto setting floor color isn't supported for terrain yet.");
            }
        }

        private void DrawBounds(Bounds bounds, Color color, Matrix4x4 localToWorldMatrix)
        {
            var center = bounds.center;
            var extents = bounds.extents;

            // 8 corners of the bounding box in local space
            var corners = new Vector3[8];
            corners[0] = new Vector3(center.x - extents.x, center.y - extents.y, center.z - extents.z); // min corner
            corners[1] = new Vector3(center.x + extents.x, center.y - extents.y, center.z - extents.z);
            corners[2] = new Vector3(center.x + extents.x, center.y + extents.y, center.z - extents.z);
            corners[3] = new Vector3(center.x - extents.x, center.y + extents.y, center.z - extents.z);
            corners[4] = new Vector3(center.x - extents.x, center.y - extents.y, center.z + extents.z);
            corners[5] = new Vector3(center.x + extents.x, center.y - extents.y, center.z + extents.z);
            corners[6] = new Vector3(center.x + extents.x, center.y + extents.y, center.z + extents.z); // max corner
            corners[7] = new Vector3(center.x - extents.x, center.y + extents.y, center.z + extents.z);

            // Transform corners to world space
            for (int i = 0; i < 8; i++)
            {
                corners[i] = localToWorldMatrix.MultiplyPoint3x4(corners[i]);
            }

            // Draw the 12 edges of the cube
            // Bottom face (z-)
            Debug.DrawLine(corners[0], corners[1], color);
            Debug.DrawLine(corners[1], corners[2], color);
            Debug.DrawLine(corners[2], corners[3], color);
            Debug.DrawLine(corners[3], corners[0], color);
            
            // Top face (z+)
            Debug.DrawLine(corners[4], corners[5], color);
            Debug.DrawLine(corners[5], corners[6], color);
            Debug.DrawLine(corners[6], corners[7], color);
            Debug.DrawLine(corners[7], corners[4], color);
            
            // Vertical edges connecting bottom and top faces
            Debug.DrawLine(corners[0], corners[4], color);
            Debug.DrawLine(corners[1], corners[5], color);
            Debug.DrawLine(corners[2], corners[6], color);
            Debug.DrawLine(corners[3], corners[7], color);
        }
    }
}