using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class TerrainSetup
{
    static TerrainSetup()
    {
        EditorApplication.delayCall += SetupTerrain;
    }

    [MenuItem("Tools/Setup Terrain Layers")]
    public static void SetupTerrain()
    {
        Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);

        if (terrains == null || terrains.Length == 0)
        {
            Debug.LogWarning("No Terrain found in the active scene. Please create a Terrain first (Right-Click Hierarchy -> 3D Object -> Terrain).");
            return;
        }

        // 1. Load Grass Layer
        TerrainLayer grassLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/_TerrainAutoUpgrade/layer_grass01b3407d0e55802f81.terrainlayer");
        if (grassLayer == null)
        {
            grassLayer = new TerrainLayer();
            grassLayer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Fantasy Forest Environment Free Sample/Textures/grass01.tga");
            grassLayer.tileSize = new Vector2(12, 12);
            AssetDatabase.CreateAsset(grassLayer, "Assets/layer_grass_created.terrainlayer");
        }
        else
        {
            grassLayer.tileSize = new Vector2(12, 12);
        }

        // 2. Load Pathway Dirt Layer
        TerrainLayer dirtLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/_TerrainAutoUpgrade/layer_dirt01b3407d0e55802f81.terrainlayer");
        if (dirtLayer == null)
        {
            dirtLayer = new TerrainLayer();
            dirtLayer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Fantasy Forest Environment Free Sample/Textures/dirt01.tga");
            dirtLayer.tileSize = new Vector2(10, 10);
            AssetDatabase.CreateAsset(dirtLayer, "Assets/layer_dirt_created.terrainlayer");
        }
        else
        {
            dirtLayer.tileSize = new Vector2(10, 10);
        }

        // 3. Load Ground/Mud Layer (for the steep hills!)
        TerrainLayer groundMudLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/_TerrainAutoUpgrade/layer_groundground_normal2023054508611406.terrainlayer");
        if (groundMudLayer == null)
        {
            groundMudLayer = new TerrainLayer();
            groundMudLayer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Cabin/Terrain/ground.png");
            groundMudLayer.normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Cabin/Terrain/ground_normal.png");
            groundMudLayer.tileSize = new Vector2(8, 8);
            AssetDatabase.CreateAsset(groundMudLayer, "Assets/layer_ground_created.terrainlayer");
        }
        else
        {
            groundMudLayer.tileSize = new Vector2(8, 8);
        }

        // Find the source terrain to copy details/trees from
        Terrain originalTerrain = null;
        foreach (Terrain t in terrains)
        {
            if (t.name == "Terrain")
            {
                originalTerrain = t;
                break;
            }
        }

        // Apply settings to all terrains found in the scene
        foreach (Terrain terrain in terrains)
        {
            if (terrain.terrainData == null) continue;

            // Assign Layers to Terrain Data
            terrain.terrainData.terrainLayers = new TerrainLayer[] { grassLayer, dirtLayer, groundMudLayer };
            
            // AUTO-TEXTURE BY SLOPE (STEEPNESS)
            AutoTextureBySlope(terrain);

            // Copy detail (grass meshes) and tree prototypes from original Terrain to new terrains
            if (originalTerrain != null && terrain != originalTerrain && originalTerrain.terrainData != null)
            {
                // Deep copy detail prototypes to force serialization refresh
                var originalDetails = originalTerrain.terrainData.detailPrototypes;
                DetailPrototype[] newDetails = new DetailPrototype[originalDetails.Length];
                for (int i = 0; i < originalDetails.Length; i++)
                {
                    newDetails[i] = new DetailPrototype();
                    newDetails[i].prototype = originalDetails[i].prototype;
                    newDetails[i].prototypeTexture = originalDetails[i].prototypeTexture;
                    newDetails[i].renderMode = originalDetails[i].renderMode;
                    newDetails[i].usePrototypeMesh = originalDetails[i].usePrototypeMesh;
                    newDetails[i].minWidth = originalDetails[i].minWidth;
                    newDetails[i].maxWidth = originalDetails[i].maxWidth;
                    newDetails[i].minHeight = originalDetails[i].minHeight;
                    newDetails[i].maxHeight = originalDetails[i].maxHeight;
                    newDetails[i].healthyColor = originalDetails[i].healthyColor;
                    newDetails[i].dryColor = originalDetails[i].dryColor;
                    newDetails[i].bendFactor = originalDetails[i].bendFactor;
                }
                terrain.terrainData.detailPrototypes = newDetails;

                // Deep copy tree prototypes to force serialization refresh
                var originalTrees = originalTerrain.terrainData.treePrototypes;
                TreePrototype[] newTrees = new TreePrototype[originalTrees.Length];
                for (int i = 0; i < originalTrees.Length; i++)
                {
                    newTrees[i] = new TreePrototype();
                    newTrees[i].prefab = originalTrees[i].prefab;
                    newTrees[i].bendFactor = originalTrees[i].bendFactor;
                }
                terrain.terrainData.treePrototypes = newTrees;

                // Auto-populate grass detail meshes on the new terrain flat areas
                AutoPaintGrassDetails(terrain);
            }

            terrain.Flush();
            EditorUtility.SetDirty(terrain.terrainData);
            EditorUtility.SetDirty(terrain);
        }

        // 6. UPGRADE FOLIAGE & GRASS MATERIALS FOR URP
        UpgradeFoliageMaterials();

        // 7. LOG DETAIL INFO FOR DEBUGGING
        if (terrains.Length > 0 && terrains[0] != null)
        {
            LogDetailInfo(terrains[0]);
        }

        AssetDatabase.SaveAssets();

        Debug.Log($"Terrain Layers and details updated successfully for {terrains.Length} terrains!");
    }

    private static void AutoTextureBySlope(Terrain terrain)
    {
        TerrainData terrainData = terrain.terrainData;
        int alphamapWidth = terrainData.alphamapWidth;
        int alphamapHeight = terrainData.alphamapHeight;

        float[,,] splatmapData = new float[alphamapWidth, alphamapHeight, terrainData.terrainLayers.Length];

        for (int y = 0; y < alphamapHeight; y++)
        {
            for (int x = 0; x < alphamapWidth; x++)
            {
                float grassWeight = 0f;
                float dirtWeight = 0f;
                float stoneWeight = 0f;

                // For Terrain (1), paint it 100% with the Mud/Ground texture
                if (terrain.name != "Terrain")
                {
                    stoneWeight = 1f; // index 2 is the mud/ground texture
                }
                else
                {
                    // Normalize coordinates for steepness calculation (0 to 1)
                    float normX = (float)x / (alphamapWidth - 1);
                    float normY = (float)y / (alphamapHeight - 1);

                    // Get slope angle (0 to 90 degrees)
                    float angle = terrainData.GetSteepness(normX, normY);

                    // Adjust paint weight based on slope angle
                    if (angle > 28f)
                    {
                        // Extremely steep: Forest Mud/Ground
                        stoneWeight = 1f;
                    }
                    else if (angle > 14f)
                    {
                        // Moderately steep: Blend dirt pathway and forest mud
                        float t = (angle - 14f) / (28f - 14f);
                        stoneWeight = t;
                        dirtWeight = 1f - t;
                    }
                    else if (angle > 4f)
                    {
                        // Soft slope: Blend grass and dirt pathway
                        float t = (angle - 4f) / (14f - 4f);
                        dirtWeight = t;
                        grassWeight = 1f - t;
                    }
                    else
                    {
                        // Flat ground: Grass
                        grassWeight = 1f;
                    }
                }

                splatmapData[y, x, 0] = grassWeight;
                splatmapData[y, x, 1] = dirtWeight;
                splatmapData[y, x, 2] = stoneWeight;
            }
        }

        // Apply splatmap back to Terrain Data
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }

    private static void AutoPaintGrassDetails(Terrain terrain)
    {
        TerrainData terrainData = terrain.terrainData;
        int detailWidth = terrainData.detailWidth;
        int detailHeight = terrainData.detailHeight;
        int layerCount = terrainData.detailPrototypes.Length;

        if (layerCount == 0) return;

        // Populate detail layer 0 (the main grass mesh) on flat areas
        int[,] detailMap = new int[detailWidth, detailHeight];

        for (int y = 0; y < detailHeight; y++)
        {
            for (int x = 0; x < detailWidth; x++)
            {
                float normX = (float)x / (detailWidth - 1);
                float normY = (float)y / (detailHeight - 1);

                float angle = terrainData.GetSteepness(normX, normY);

                // Scatter grass details on flat areas (slope < 4 degrees) with a 15% density
                if (angle < 4f && Random.value < 0.15f)
                {
                    detailMap[y, x] = Random.Range(1, 3); // 1 or 2 grass clumps
                }
                else
                {
                    detailMap[y, x] = 0;
                }
            }
        }

        // Apply detail map to first detail layer
        terrainData.SetDetailLayer(0, 0, 0, detailMap);
    }

    private static void UpgradeFoliageMaterials()
    {
        string[] matPaths = new string[]
        {
            "Assets/Fantasy Forest Environment Free Sample/Materials/grassmesh.mat",
            "Assets/Fantasy Forest Environment Free Sample/Materials/grass01.mat",
            "Assets/Fantasy Forest Environment Free Sample/Materials/grass01_b.mat",
            "Assets/Fantasy Forest Environment Free Sample/Materials/tree_branches.mat",
            "Assets/Fantasy Forest Environment Free Sample/Materials/bark01_bottom.mat",
            "Assets/Fantasy Forest Environment Free Sample/Materials/dirt01.mat"
        };

        Shader simpleLitShader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (simpleLitShader == null)
        {
            Debug.LogWarning("Could not find 'Universal Render Pipeline/Simple Lit' shader. Make sure URP is installed.");
            return;
        }

        foreach (string path in matPaths)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            mat.shader = simpleLitShader;

            // Load and assign correct texture programmatically to prevent missing textures
            string texPath = "";
            if (path.Contains("grassmesh")) texPath = "Assets/Fantasy Forest Environment Free Sample/Textures/grassmesh.png";
            else if (path.Contains("grass01_b")) texPath = "Assets/Fantasy Forest Environment Free Sample/Textures/grass01_b.tga";
            else if (path.Contains("grass01")) texPath = "Assets/Fantasy Forest Environment Free Sample/Textures/grass01.tga";
            else if (path.Contains("tree_branches")) texPath = "Assets/Fantasy Forest Environment Free Sample/Textures/tree_branches.png";
            else if (path.Contains("bark01_bottom")) texPath = "Assets/Fantasy Forest Environment Free Sample/Textures/bark01_bottom.tga";
            else if (path.Contains("dirt01")) texPath = "Assets/Fantasy Forest Environment Free Sample/Textures/dirt01.tga";

            if (!string.IsNullOrEmpty(texPath))
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (tex != null)
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.SetTexture("_MainTex", tex);
                }
            }

            // If it's a foliage material, configure it for cutout transparency and double-sided rendering
            if (path.Contains("grass") || path.Contains("branches"))
            {
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", 0.4f);
                mat.SetFloat("_Surface", 0f); // Opaque (Cutout)
                mat.SetOverrideTag("RenderType", "TransparentCutout");
                
                // Double sided (Cull = 0)
                mat.SetFloat("_Cull", 0f);
                
                // Set appropriate blend modes for Cutout
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }
            else
            {
                // Standard Opaque for bark/dirt
                mat.SetFloat("_AlphaClip", 0f);
                mat.SetFloat("_Surface", 0f);
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.SetFloat("_Cull", 2f); // Back culling
                mat.DisableKeyword("_ALPHATEST_ON");
            }

            EditorUtility.SetDirty(mat);
        }
    }

    private static void LogDetailInfo(Terrain terrain)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Terrain: {terrain.name}");
        if (terrain.terrainData != null)
        {
            var prototypes = terrain.terrainData.detailPrototypes;
            sb.AppendLine($"Detail Prototypes Count: {prototypes.Length}");
            for (int i = 0; i < prototypes.Length; i++)
            {
                var proto = prototypes[i];
                sb.AppendLine($"Detail [{i}]:");
                sb.AppendLine($"  renderMode: {proto.renderMode}");
                sb.AppendLine($"  usePrototypeMesh: {proto.usePrototypeMesh}");
                sb.AppendLine($"  prototype Prefab: {(proto.prototype != null ? proto.prototype.name : "null")}");
                sb.AppendLine($"  prototypeTexture: {(proto.prototypeTexture != null ? proto.prototypeTexture.name : "null")}");
                if (proto.prototype != null)
                {
                    var renderer = proto.prototype.GetComponentInChildren<MeshRenderer>();
                    if (renderer != null)
                    {
                        sb.AppendLine($"  Prefab MeshRenderer Materials:");
                        foreach (var mat in renderer.sharedMaterials)
                        {
                            if (mat != null)
                            {
                                sb.AppendLine($"    - Name: {mat.name}, Shader: {mat.shader.name}, Texture: {(mat.mainTexture != null ? mat.mainTexture.name : "null")}");
                            }
                            else
                            {
                                sb.AppendLine($"    - null material");
                            }
                        }
                    }
                    else
                    {
                        sb.AppendLine($"  Prefab has no MeshRenderer in children!");
                    }
                }
            }
        }
        else
        {
            sb.AppendLine("TerrainData is null");
        }
        try
        {
            System.IO.File.WriteAllText("Assets/detail_info.txt", sb.ToString());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to write detail_info.txt: {e.Message}");
        }
    }
}
