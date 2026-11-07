using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Moba.EditorTools
{
    /// <summary>
    /// Меню MOBA → Build Prototype Scene.
    /// Генерирует материалы, конфиги, префабы и сцену Assets/Scenes/Main.unity
    /// (карта, NavMesh, базы, башни, фонтаны, герои, спавнеры, камера, HUD).
    /// Повторный запуск пересоздаёт папку Assets/Generated и сцену.
    /// </summary>
    public static class MobaSceneBuilder
    {
        const string Gen = "Assets/Generated";
        const string ScenePath = "Assets/Scenes/Main.unity";

        // Карта (ось X — линия, Z — ширина)
        const float BaseX = 50f;
        const float TowerX = 30f;
        const float FountainX = 42f;
        const float SpawnX = 40f;

        static Material blueMat, redMat, groundMat, wallMat, fountainMat, markerMat,
                        barBackMat, barFillMat, attackProjMat, boltMat, fxMat;

        static Projectile attackProjectile, boltProjectile;
        static GameObject shockFx, meleePrefab, rangedPrefab;
        static UnitConfig heroCfg, botCfg, meleeCfg, rangedCfg, towerCfg, baseCfg;
        static AbilityData qAbility, wAbility, eAbility;

        [MenuItem("MOBA/Build Prototype Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (AssetDatabase.IsValidFolder(Gen)) AssetDatabase.DeleteAsset(Gen);

            EnsureFolder(Gen + "/Materials");
            EnsureFolder(Gen + "/Configs");
            EnsureFolder(Gen + "/Prefabs");
            EnsureFolder("Assets/Scenes");

            CreateMaterials();
            CreateEffectPrefabs();
            CreateConfigs();
            CreateAbilities();
            CreateMinionPrefabs();

            // --- окружение ---
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

            var env = new GameObject("Environment").transform;
            Cube("Ground", env, new Vector3(0f, -0.5f, 0f), new Vector3(124f, 1f, 44f), groundMat);
            Cube("Wall_N", env, new Vector3(0f, 2f, 22.5f), new Vector3(126f, 4f, 1f), wallMat);
            Cube("Wall_S", env, new Vector3(0f, 2f, -22.5f), new Vector3(126f, 4f, 1f), wallMat);
            Cube("Wall_E", env, new Vector3(62.5f, 2f, 0f), new Vector3(1f, 4f, 46f), wallMat);
            Cube("Wall_W", env, new Vector3(-62.5f, 2f, 0f), new Vector3(1f, 4f, 46f), wallMat);
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    Cube("LaneWall", env, new Vector3(sx * 18f, 1.5f, sz * 7.5f), new Vector3(24f, 3f, 2f), wallMat);
                    Cube("Jungle", env, new Vector3(sx * 24f, 1f, sz * 15f), new Vector3(8f, 2f, 8f), wallMat);
                }
            }

            // --- вейпоинты линии ---
            // Общие точки -20, 0, 20; у каждой команды своя конечная точка перед вражеской базой.
            var laneRoot = new GameObject("Lane").transform;
            Transform Waypoint(string wpName, float x)
            {
                var wp = new GameObject(wpName).transform;
                wp.SetParent(laneRoot);
                wp.position = new Vector3(x, 0f, 0f);
                return wp;
            }
            Transform wpA = Waypoint("WP_Left", -20f);
            Transform wpB = Waypoint("WP_Mid", 0f);
            Transform wpC = Waypoint("WP_Right", 20f);
            Transform endBlue = Waypoint("WP_EndForBlue", SpawnX);
            Transform endRed = Waypoint("WP_EndForRed", -SpawnX);
            var blueLane = new[] { wpA, wpB, wpC, endBlue };
            var redLane = new[] { wpC, wpB, wpA, endRed };

            // --- здания (тела) до запекания NavMesh ---
            var structures = new GameObject("Structures").transform;
            var blueBase = BuildBase("Base_Blue", TeamId.Blue, new Vector3(-BaseX, 0f, 0f), blueMat, structures);
            var redBase = BuildBase("Base_Red", TeamId.Red, new Vector3(BaseX, 0f, 0f), redMat, structures);
            var blueTower = BuildTower("Tower_Blue", TeamId.Blue, new Vector3(-TowerX, 0f, 4f), blueMat, structures);
            var redTower = BuildTower("Tower_Red", TeamId.Red, new Vector3(TowerX, 0f, -4f), redMat, structures);

            // --- NavMesh ---
            var navGo = new GameObject("NavMesh");
            var surface = navGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();
            if (surface.navMeshData != null)
            {
                AssetDatabase.CreateAsset(surface.navMeshData, Gen + "/NavMeshData.asset");
                EditorUtility.SetDirty(surface);
            }
            else
            {
                Debug.LogError("MOBA: NavMesh was not baked. Check that the AI Navigation package is installed.");
            }

            // --- всё, что не должно попасть в NavMesh, создаём после запекания ---
            foreach (var go in new[] { blueBase, redBase, blueTower, redTower })
                AddHealthBar(go, go.GetComponent<Unit>().kind == UnitKind.Base ? 6.5f : 8f, 3f);

            BuildFountain("Fountain_Blue", TeamId.Blue, new Vector3(-FountainX, 0f, 0f));
            var redFountain = BuildFountain("Fountain_Red", TeamId.Red, new Vector3(FountainX, 0f, 0f));

            var blueSpawn = new GameObject("Spawn_Blue").transform;
            blueSpawn.position = new Vector3(-SpawnX, 0f, 0f);
            blueSpawn.rotation = Quaternion.LookRotation(Vector3.right);
            var redSpawn = new GameObject("Spawn_Red").transform;
            redSpawn.position = new Vector3(SpawnX, 0f, 0f);
            redSpawn.rotation = Quaternion.LookRotation(Vector3.left);

            var blueHeroSpawn = new GameObject("HeroSpawn_Blue").transform;
            blueHeroSpawn.position = new Vector3(-FountainX + 1f, 0f, 4f);
            var redHeroSpawn = new GameObject("HeroSpawn_Red").transform;
            redHeroSpawn.position = new Vector3(FountainX - 1f, 0f, -4f);

            // --- спавнеры миньонов ---
            MakeSpawner("Spawner_Blue", TeamId.Blue, blueSpawn, blueLane, blueMat);
            MakeSpawner("Spawner_Red", TeamId.Red, redSpawn, redLane, redMat);

            // --- герои ---
            var player = BuildHero("Hero_Player", TeamId.Blue, blueHeroSpawn.position, heroCfg, blueMat,
                                   true, null, blueHeroSpawn, null);
            BuildHero("Hero_Bot", TeamId.Red, redHeroSpawn.position, botCfg, redMat,
                      false, redLane, redHeroSpawn, redFountain.transform);

            // --- камера и матч ---
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.09f, 0.12f);
            cam.farClipPlane = 200f;
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<TopDownCamera>();
            follow.target = player.transform;
            camGo.transform.position = player.transform.position + follow.offset;
            camGo.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

            var matchGo = new GameObject("Match");
            var match = matchGo.AddComponent<MatchController>();
            match.playerTeam = TeamId.Blue;
            matchGo.AddComponent<MatchHud>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = player;
            Debug.Log("MOBA: prototype scene built at " + ScenePath + ". Press Play.");
        }

        // ---------------------------------------------------------------- материалы

        static Material MakeMat(string name, Color color, string shaderName = "Standard")
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(mat, Gen + "/Materials/" + name + ".mat");
            return mat;
        }

        static void CreateMaterials()
        {
            blueMat = MakeMat("Blue", new Color(0.2f, 0.45f, 1f));
            redMat = MakeMat("Red", new Color(1f, 0.25f, 0.25f));
            groundMat = MakeMat("Ground", new Color(0.25f, 0.42f, 0.25f));
            wallMat = MakeMat("Wall", new Color(0.35f, 0.35f, 0.4f));
            fountainMat = MakeMat("Fountain", new Color(0.3f, 0.9f, 0.9f), "Unlit/Color");
            markerMat = MakeMat("Marker", new Color(0.95f, 0.95f, 0.95f));
            barBackMat = MakeMat("BarBack", new Color(0.05f, 0.05f, 0.05f), "Unlit/Color");
            barFillMat = MakeMat("BarFill", new Color(0.25f, 0.9f, 0.3f), "Unlit/Color");
            attackProjMat = MakeMat("AttackProjectile", new Color(1f, 0.9f, 0.3f), "Unlit/Color");
            boltMat = MakeMat("Bolt", new Color(0.4f, 0.95f, 1f), "Unlit/Color");
            fxMat = MakeMat("ShockFx", new Color(0.6f, 0.9f, 1f), "Unlit/Color");
        }

        // ---------------------------------------------------------------- префабы

        static GameObject SavePrefab(GameObject go, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, Gen + "/Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static Projectile MakeProjectile(string name, float scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.localScale = Vector3.one * scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.AddComponent<Projectile>();
            return SavePrefab(go, name).GetComponent<Projectile>();
        }

        static void CreateEffectPrefabs()
        {
            attackProjectile = MakeProjectile("AttackProjectile", 0.4f, attackProjMat);
            boltProjectile = MakeProjectile("BoltProjectile", 0.8f, boltMat);

            var fx = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fx.name = "ShockwaveFx";
            fx.transform.localScale = new Vector3(1f, 0.025f, 1f);
            fx.GetComponent<Renderer>().sharedMaterial = fxMat;
            Object.DestroyImmediate(fx.GetComponent<Collider>());
            fx.AddComponent<TimedFx>();
            shockFx = SavePrefab(fx, "ShockwaveFx");
        }

        // ---------------------------------------------------------------- данные

        static UnitConfig MakeUnitConfig(string file, string display, float hp, float speed, float radius,
            float dmg, float range, float cooldown, float aggro,
            Projectile proj = null, float projSpeed = 14f, float projHeight = 1.2f)
        {
            var c = ScriptableObject.CreateInstance<UnitConfig>();
            c.displayName = display;
            c.maxHealth = hp;
            c.moveSpeed = speed;
            c.radius = radius;
            c.attackDamage = dmg;
            c.attackRange = range;
            c.attackCooldown = cooldown;
            c.aggroRange = aggro;
            c.projectilePrefab = proj;
            c.projectileSpeed = projSpeed;
            c.projectileHeight = projHeight;
            AssetDatabase.CreateAsset(c, Gen + "/Configs/" + file + ".asset");
            return c;
        }

        static void CreateConfigs()
        {
            heroCfg = MakeUnitConfig("Hero_Player", "Hero", 600f, 5.5f, 0.6f, 35f, 6f, 0.9f, 12f, attackProjectile, 16f, 1.5f);
            botCfg = MakeUnitConfig("Hero_Bot", "Bot", 600f, 5.2f, 0.6f, 35f, 6f, 0.9f, 12f, attackProjectile, 16f, 1.5f);
            meleeCfg = MakeUnitConfig("Minion_Melee", "Minion", 120f, 3.2f, 0.4f, 12f, 1.8f, 1f, 8f);
            rangedCfg = MakeUnitConfig("Minion_Ranged", "Archer", 80f, 3.2f, 0.4f, 14f, 6f, 1.3f, 9f, attackProjectile, 12f, 1.2f);
            towerCfg = MakeUnitConfig("Tower", "Tower", 1200f, 0f, 1.5f, 60f, 12f, 1.2f, 12f, attackProjectile, 18f, 6f);
            baseCfg = MakeUnitConfig("Base", "Base", 2000f, 0f, 3.5f, 0f, 0f, 1f, 0f);
        }

        static AbilityData MakeAbility(string file, string title, AbilityType type, float cooldown, float mana,
            float damage, float range, float radius, float speed, float duration)
        {
            var a = ScriptableObject.CreateInstance<AbilityData>();
            a.abilityName = title;
            a.type = type;
            a.cooldown = cooldown;
            a.manaCost = mana;
            a.damage = damage;
            a.range = range;
            a.radius = radius;
            a.speed = speed;
            a.duration = duration;
            AssetDatabase.CreateAsset(a, Gen + "/Configs/" + file + ".asset");
            return a;
        }

        static void CreateAbilities()
        {
            qAbility = MakeAbility("Ability_Q_Bolt", "Bolt", AbilityType.Skillshot, 4f, 25f, 70f, 14f, 0.8f, 22f, 0f);
            qAbility.projectilePrefab = boltProjectile;

            wAbility = MakeAbility("Ability_W_Dash", "Dash", AbilityType.Dash, 8f, 30f, 0f, 7f, 0f, 0f, 0.2f);

            eAbility = MakeAbility("Ability_E_Shockwave", "Shock", AbilityType.Shockwave, 10f, 40f, 60f, 0f, 5f, 0f, 0f);
            eAbility.slowFactor = 0.5f;
            eAbility.slowDuration = 2f;
            eAbility.effectPrefab = shockFx;

            EditorUtility.SetDirty(qAbility);
            EditorUtility.SetDirty(eAbility);
        }

        // ---------------------------------------------------------------- юниты

        static void ConfigureAgent(GameObject root, UnitConfig cfg, float height, int priority)
        {
            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = cfg.radius;
            agent.height = height;
            agent.speed = cfg.moveSpeed;
            agent.acceleration = 40f;
            agent.angularSpeed = 600f;
            agent.stoppingDistance = 0.1f;
            agent.avoidancePriority = priority;
        }

        static GameObject BuildMinion(string name, UnitConfig cfg, PrimitiveType shape, float scale)
        {
            var root = new GameObject(name);
            var body = GameObject.CreatePrimitive(shape);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = Vector3.one * scale;
            body.transform.localPosition = new Vector3(0f, scale, 0f);
            body.GetComponent<Renderer>().sharedMaterial = blueMat;

            var unit = root.AddComponent<Unit>();
            unit.team = TeamId.Blue;
            unit.kind = UnitKind.Minion;
            unit.config = cfg;
            unit.tintedRenderers = new[] { body.GetComponent<Renderer>() };

            ConfigureAgent(root, cfg, scale * 2f, 50);
            root.AddComponent<Mover>();
            root.AddComponent<StatusEffects>();
            root.AddComponent<Combat>();
            root.AddComponent<MinionAI>();
            AddHealthBar(root, scale * 2f + 0.6f, 1.2f);
            return root;
        }

        static void CreateMinionPrefabs()
        {
            meleePrefab = SavePrefab(BuildMinion("MinionMelee", meleeCfg, PrimitiveType.Capsule, 0.9f), "MinionMelee");
            rangedPrefab = SavePrefab(BuildMinion("MinionRanged", rangedCfg, PrimitiveType.Cylinder, 0.8f), "MinionRanged");
        }

        static GameObject BuildStructure(string name, TeamId team, UnitKind kind, UnitConfig cfg, Vector3 pos,
                                         Material mat, Transform parent, Vector3 bodyScale, float bodyY)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = pos;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, bodyY, 0f);
            body.transform.localScale = bodyScale;
            body.GetComponent<Renderer>().sharedMaterial = mat;

            var unit = root.AddComponent<Unit>();
            unit.team = team;
            unit.kind = kind;
            unit.config = cfg;
            unit.tintedRenderers = new[] { body.GetComponent<Renderer>() };
            return root;
        }

        static GameObject BuildBase(string name, TeamId team, Vector3 pos, Material mat, Transform parent)
        {
            return BuildStructure(name, team, UnitKind.Base, baseCfg, pos, mat, parent, new Vector3(7f, 2.5f, 7f), 2.5f);
        }

        static GameObject BuildTower(string name, TeamId team, Vector3 pos, Material mat, Transform parent)
        {
            var root = BuildStructure(name, team, UnitKind.Tower, towerCfg, pos, mat, parent, new Vector3(3f, 3.5f, 3f), 3.5f);
            root.AddComponent<Combat>();
            root.AddComponent<TowerAI>();
            return root;
        }

        static GameObject BuildFountain(string name, TeamId team, Vector3 pos)
        {
            var root = new GameObject(name);
            root.transform.position = pos;

            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Disc";
            disc.transform.SetParent(root.transform, false);
            disc.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            disc.transform.localScale = new Vector3(16f, 0.02f, 16f);
            disc.GetComponent<Renderer>().sharedMaterial = fountainMat;
            Object.DestroyImmediate(disc.GetComponent<Collider>());

            var f = root.AddComponent<Fountain>();
            f.team = team;
            f.radius = 8f;
            return root;
        }

        static GameObject BuildHero(string name, TeamId team, Vector3 pos, UnitConfig cfg, Material mat,
                                    bool isPlayer, Transform[] lane, Transform spawn, Transform fountain)
        {
            var root = new GameObject(name);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.LookRotation(team == TeamId.Blue ? Vector3.right : Vector3.left);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = Vector3.one * 1.2f;
            body.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            body.GetComponent<Renderer>().sharedMaterial = mat;

            // Маркер «лица», чтобы было видно направление героя
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Marker";
            marker.transform.SetParent(root.transform, false);
            marker.transform.localPosition = new Vector3(0f, 1.7f, 0.55f);
            marker.transform.localScale = new Vector3(0.35f, 0.25f, 0.4f);
            marker.GetComponent<Renderer>().sharedMaterial = markerMat;
            Object.DestroyImmediate(marker.GetComponent<Collider>());

            var unit = root.AddComponent<Unit>();
            unit.team = team;
            unit.kind = UnitKind.Hero;
            unit.config = cfg;
            unit.tintedRenderers = new[] { body.GetComponent<Renderer>() };

            ConfigureAgent(root, cfg, 2.4f, 40);
            root.AddComponent<Mover>();
            root.AddComponent<StatusEffects>();
            root.AddComponent<Combat>();

            var abilities = root.AddComponent<HeroAbilities>();
            abilities.abilities = new[] { qAbility, wAbility, eAbility };

            var respawn = root.AddComponent<HeroRespawn>();
            respawn.spawnPoint = spawn;

            if (isPlayer)
            {
                root.AddComponent<PlayerHero>();
            }
            else
            {
                var bot = root.AddComponent<BotHero>();
                bot.lane = lane;
                bot.fountainPoint = fountain;
            }

            AddHealthBar(root, 3.3f, 2f);
            return root;
        }

        static void MakeSpawner(string name, TeamId team, Transform spawnPoint, Transform[] lane, Material teamMat)
        {
            var go = new GameObject(name);
            var sp = go.AddComponent<MinionSpawner>();
            sp.team = team;
            sp.meleePrefab = meleePrefab;
            sp.rangedPrefab = rangedPrefab;
            sp.teamMaterial = teamMat;
            sp.spawnPoint = spawnPoint;
            sp.lane = lane;
        }

        // ---------------------------------------------------------------- вспомогательное

        static void AddHealthBar(GameObject root, float height, float width)
        {
            var barRoot = new GameObject("HealthBar");
            barRoot.transform.SetParent(root.transform, false);
            barRoot.transform.localPosition = new Vector3(0f, height, 0f);

            float h = 0.18f;
            var back = GameObject.CreatePrimitive(PrimitiveType.Quad);
            back.name = "Back";
            back.transform.SetParent(barRoot.transform, false);
            back.transform.localScale = new Vector3(width + 0.06f, h + 0.06f, 1f);
            back.GetComponent<Renderer>().sharedMaterial = barBackMat;
            Object.DestroyImmediate(back.GetComponent<Collider>());

            var fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fill.name = "Fill";
            fill.transform.SetParent(barRoot.transform, false);
            fill.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            fill.transform.localScale = new Vector3(width, h, 1f);
            fill.GetComponent<Renderer>().sharedMaterial = barFillMat;
            Object.DestroyImmediate(fill.GetComponent<Collider>());

            var bar = barRoot.AddComponent<HealthBar>();
            bar.fill = fill.transform;
            bar.width = width;
            bar.height = h;
        }

        static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
