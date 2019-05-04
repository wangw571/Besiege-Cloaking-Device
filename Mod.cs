using Modding;
using UnityEngine;
using Modding.Blocks;
using System.Collections;
using System.Collections.Generic;
using Besiege.Networking;

namespace Cloaking_Block
{
    public static class Messages
    {
        // Message for knowing a simulation started
        public static MessageType CloakSimuStart;
        // Message for syncing the cloak progress/position
        public static MessageType CloakProgress;
    }

    public class Cloaking_Block : ModEntryPoint
    {
        // This is called when the mod is first loaded.
        public override void OnLoad()
        {
            // Initialize messages
            Messages.CloakProgress = ModNetworking.CreateMessageType(DataType.Block, DataType.Single, DataType.Boolean, DataType.Single, DataType.Vector3, DataType.Boolean);
            // Script after message has been received
            ModNetworking.Callbacks[Messages.CloakProgress] += message2 =>
            {
                // Save and convert data that just received from message
                Block block = (Block)message2.GetData(0);
                float sizee = (float)message2.GetData(1);
                bool Activation = (bool)message2.GetData(2);
                float timer = (float)message2.GetData(3);
                Vector3 position = (Vector3)message2.GetData(4);
                bool flashish = (bool)message2.GetData(5);
                // I used static method so I can get the desired detector script
                // Note that block.SimBlock is the simulating instance of desired block. if not using SimBlock then the building instance will be given.
                StealthCloakFieldScript clk = StealthCloakFieldScriptMP.getSCFS(block.SimBlock);
                if (clk.GetType() == typeof(StealthCloakFieldScriptMP))
                {
                    // Some method for syncing
                    ((StealthCloakFieldScriptMP)clk).CloakSizeModifier(sizee, Activation, timer, position, flashish);
                }
            };

            Messages.CloakSimuStart = ModNetworking.CreateMessageType(DataType.Block);
            ModNetworking.Callbacks[Messages.CloakSimuStart] += message4 =>
            {
                Block block = (Block)message4.GetData(0);
                // The script on cloak block in client
                PROCloakScript clk = block.SimBlock.GameObject.GetComponent<PROCloakScript>();
                // Use the initialization
                clk.Initialization();
            };
        }
    }
    // Ignore this class
    class pp
    {
        public static void log(string o)
        {
            BesiegeConsoleController.ShowMessage(o);
        }
        public static string GetGameObjectPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }
    // If you need documentation about any of these values or the mod loader
    // in general, take a look at https://spaar.github.io/besiege-modloader.

    public class PROCloakScript : Modding.BlockScript
    {

        private MKey ActivationKey;
        private MSlider CloakFieldSize;
        private MSlider CloakExtendingTime;
        private MToggle DirectionVisualization;


        private AudioSource CloakAudio;
        private AudioSource DecloakAudio;
        private float size;
        private float FieldSize;
        private bool Activated;
        private bool bumpFixedUpdate;
        private float ReactivationTimer;
        private Material InvBumpTex;
        private Material InvTex;
        private float CoolDownTime;
        private bool flashing = false;
        public float 记录器 = 0;
        public GameObject FieldDetector;
        public void Cloak(bool Activation)
        {
            Activated = Activation;
            ReactivationTimer = 0;
            flashing = true;
            if (!Activation) DecloakAudio.Play();
            else CloakAudio.Play();
        }
        public override void SafeAwake()
        {
            ActivationKey = AddKey("Activation", //按键信息
                                 "ACT",           //名字
                                 KeyCode.P);       //默认按键
            CloakFieldSize = AddSlider("Clock Field Size", "size", 10, 0.01f, 50);
            CloakExtendingTime = AddSlider("Clock Field Extend Time", "time", 3, 0, 20);
            DirectionVisualization = AddToggle("Visual Effect for activition", "vfx", true);
        }

        public override void OnSimulateStart()
        {
            // This will be execute on host's BlockScript
            Initialization();
            // Therefore message will be sent from here to let all other clients' cloak block to initialize 
            Message cloakStarted = Messages.CloakSimuStart.CreateMessage(Block.From(this));
            ModNetworking.SendInSimulation(cloakStarted);

        }

        public void Initialization()
        {
            if (FieldDetector != null) return; // Avoid initializaiton running multiple times 
            ReactivationTimer = CloakExtendingTime.Value;
            AudioInit();
            FieldSize = CloakFieldSize.Value;
            CoolDownTime = 0;
            InvBumpTex = new Material(Shader.Find("FX/Glass/Stained BumpDistort"));

            InvBumpTex.color = new Color(1, 1, 1, 0);
            InvBumpTex.mainTexture = ModResource.GetTexture("Cloak disturb.png");
            InvBumpTex.SetTexture("_BumpMap", ModResource.GetTexture("Cloak disturb.png"));
            InvTex = new Material(Shader.Find("Transparent/Diffuse"));
            InvTex.color = new Color(1, 1, 1, 0);
            InvTex.mainTexture = ModResource.GetTexture("ALL BLACK.jpg");

            FieldDetector = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            FieldDetector.name = "StealthFieldDetector";
            FieldDetector.GetComponent<SphereCollider>().isTrigger = true;
            FieldDetector.transform.position = Vector3.down * 999999;
            Destroy(FieldDetector.GetComponent<Renderer>());

            // Polymorphism
            StealthCloakFieldScript a = null;

            // Decide whether create a MP version of detector or a normal version. Note that host will still use normal version.
            if (StatMaster.isMP && StatMaster.isClient)a = FieldDetector.AddComponent<StealthCloakFieldScriptMP>();
            else a = FieldDetector.AddComponent<StealthCloakFieldScript>();

            a.setCloakTimer(CloakExtendingTime.Value);
            a.useFlashVFX = this.DirectionVisualization.IsActive;

            // Use the static method to store the detector instance
            StealthCloakFieldScript.PushInNewSCFS(Block.From(this).SimBlock, a);
            // This is for deletion usage
            a.MyBeloning = this;

            // Adding Cloak script to other blocks
            foreach (MeshRenderer renderer in Machine.SimulationMachine.GetComponentsInChildren<MeshRenderer>())
            {
                getCloakScript(renderer);
            }
        }

        private void AudioInit()
        {
            CloakAudio = this.gameObject.AddComponent<AudioSource>();
            CloakAudio.clip = ModResource.GetAudioClip("Cloaking.ogg");
            CloakAudio.loop = false;
            CloakAudio.volume = 1f;
            DecloakAudio = this.gameObject.AddComponent<AudioSource>();
            DecloakAudio.clip = ModResource.GetAudioClip("Decloaking.ogg");
            DecloakAudio.loop = false;
            DecloakAudio.volume = 1f;
        }

        public void getCloakScript(Renderer REDer)
        {
            if (!REDer.GetComponent<BeingPROCloakedScript>())
            {
                BeingPROCloakedScript BPROCS = REDer.gameObject.AddComponent<BeingPROCloakedScript>();
                BPROCS.duration = CloakExtendingTime.Value;
                BPROCS.InvisibleBumpedMat = (InvBumpTex);
                BPROCS.InvisibleBumpedMat.mainTexture = (InvBumpTex.mainTexture);
                BPROCS.InvisibleMat = (InvTex);
                BPROCS.InvisibleMat.mainTexture = (InvTex.mainTexture);
                // Due to detection mechanism, I have to make sure every block has a collider for cloaking detection
                BPROCS.addColliderToMe();
            }

        }

        public override void SimulateUpdateAlways()
        {
            // If key pressed, do the cloak things
            if (ActivationKey.IsReleased && ReactivationTimer > CoolDownTime)
            {
                Cloak(!Activated);
            }
        }
        public override void SimulateFixedUpdateAlways()
        {
            // Things that are really need to be synced
            ReactivationTimer += Time.fixedDeltaTime * (flashing && DirectionVisualization.IsActive? 5 : 1);

            FieldSize = 2 * CloakFieldSize.Value;

            if (Activated)
            {
                size = FieldSize * Mathf.Min(ReactivationTimer, CloakExtendingTime.Value) / CloakExtendingTime.Value;
            }
            else
            {
                size = FieldSize * (1 - Mathf.Min(ReactivationTimer, CloakExtendingTime.Value) / CloakExtendingTime.Value);
            }

            if(flashing && DirectionVisualization.IsActive && ReactivationTimer >= CloakExtendingTime.Value)
            {
                flashing = false;
                ReactivationTimer = 0;
            }

            CloakSizeModifier(size, flashing);
            // Bumped here to reduce network cost
            if (bumpFixedUpdate)
            {
                Message cloakInfoP = Messages.CloakProgress.CreateMessage(Block.From(this), size, Activated, ReactivationTimer, this.transform.position, flashing);
                ModNetworking.SendInSimulation(cloakInfoP);
            }
            bumpFixedUpdate = !bumpFixedUpdate;
        }

        public void CloakSizeModifier(float size, bool flash)
        {
            if(FieldDetector == null)
            {
                Initialization();
                return;
            }
            if (Activated || ReactivationTimer < CloakExtendingTime.Value)
            {
                FieldDetector.transform.localScale = Vector3.one * size;
                FieldDetector.transform.position = this.transform.position;
            }
            else
            {
                FieldDetector.transform.position = new Vector3(0, 999, 0) * -1000;
            }
            FieldDetector.GetComponent<StealthCloakFieldScript>().Flashing = flash;
        }

        public override void OnSimulateStop()
        {
            Destroy(FieldDetector);
        }
    }
    public class BeingPROCloakedScript : MonoBehaviour
    {
        private Material OriginalMat;
        public Material InvisibleBumpedMat;
        public Material InvisibleMat;
        private Color OrigColor = new Color();
        public float duration;
        public Vector3 MyPos;
        public bool InsideAField = false;
        public float 渐变数值;
        public Vector2 BumpOFFSET;
        public Vector2 BumpOFFSET惯性;
        private string Oldname;
        public void Start()
        {
            MyPos = this.transform.position;
            渐变数值 = 0;
            InsideAField = false;
            OriginalMat = new Material(Material.Instantiate(GetComponent<Renderer>().material));
            OrigColor = GetComponent<Renderer>().material.color;
            BumpOFFSET = Vector2.zero;
            BumpOFFSET惯性 = Vector2.zero;
            if (this.GetComponentInParent<Rigidbody>() != null)
                Oldname = this.GetComponentInParent<Rigidbody>().gameObject.name;
            else if (this.GetComponentInParent<MyBlockInfo>())
                Oldname = this.GetComponentInParent<MyBlockInfo>().gameObject.name;
            //DontDestroyOnLoad(InvisibleBumpedMat);
            //DontDestroyOnLoad(InvisibleMat);
            //DontDestroyOnLoad(OriginalMat);
        }
        public void addColliderToMe()
        {
            // For detection thingy
            Collider bx = this.gameObject.GetComponent<Collider>();
            if (!bx)
            {
                bx = this.gameObject.AddComponent<BoxCollider>();
                bx.enabled = true;
                bx.isTrigger = true;
            }
            else if (!bx.enabled)
            {
                bx.enabled = true;
                bx.isTrigger = true;
            }
        }
        public void FixedUpdate()
        {
            addColliderToMe();
            BumpOFFSET惯性 += new Vector2(Random.value - 0.5f, Random.value - 0.5f) * 0.001f;
            BumpOFFSET惯性 = new Vector2(Mathf.Clamp(BumpOFFSET惯性.x, -0.02f, 0.02f), Mathf.Clamp(BumpOFFSET惯性.y, -0.02f, 0.02f));
            BumpOFFSET += BumpOFFSET惯性;
            MyPos = this.transform.position;
            if (InsideAField) 渐变数值 += Time.fixedDeltaTime / duration;
            if (!InsideAField) 渐变数值 -= Time.fixedDeltaTime / duration;
            渐变数值 = Mathf.Clamp01(渐变数值);
            GameObject thisGameObj;
            if (this.GetComponentInParent<Rigidbody>() != null)
                thisGameObj = this.GetComponentInParent<Rigidbody>().gameObject;
            else if (this.GetComponentInParent<MyBlockInfo>())
                thisGameObj = this.GetComponentInParent<MyBlockInfo>().gameObject;
            else return;

            if (InsideAField)
            {
                Cloaking(渐变数值 * 2, OrigColor, BumpOFFSET);
            }
            else
            {
                Decloaking(渐变数值 * 2, OrigColor);
            }
            InsideAField = false;
        }
        public void Flash()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (OrigColor == renderer.material.color)
            {
                renderer.material.color = renderer.material.color * 999;
            }
            else UnDoFlash(renderer);
        }
        private void UnDoFlash(Renderer rder)
        {
            return;
            //rder.material.color = Color.Lerp(rder.material.color, Color.black, 0.02f);
        }
        void Decloaking(float ProgressTimesBy2, Color OriginalColor)
        {
            ProgressTimesBy2 = Mathf.Clamp(ProgressTimesBy2, 0, 2);


            if (ProgressTimesBy2 > 1.5f)
            {
                GetComponent<Renderer>().material.SetColor("_BumpMap", Color.black * (ProgressTimesBy2 - 1.5f) * 4);
                if (GetComponent<Renderer>().material.shader != InvisibleBumpedMat.shader)
                {
                    GetComponent<Renderer>().material.shader = InvisibleBumpedMat.shader;
                    GetComponent<Renderer>().material.mainTexture = null;
                    GetComponent<Renderer>().material.SetTexture("_BumpMap", InvisibleBumpedMat.GetTexture("_BumpMap"));
                }
            }
            else if (ProgressTimesBy2 > 1f)
            {
                GetComponent<Renderer>().material.color = Color.black * (1.5f - ProgressTimesBy2) * 4;
                if (GetComponent<Renderer>().material.shader != InvisibleMat.shader)
                {
                    GetComponent<Renderer>().material.shader = InvisibleMat.shader;
                    GetComponent<Renderer>().material.mainTexture = InvisibleMat.mainTexture;
                    GetComponent<Renderer>().material.SetTexture("_BumpMap", InvisibleBumpedMat.mainTexture);
                }
            }

            else if (ProgressTimesBy2 > 0)
            {
                GetComponent<Renderer>().material.color = OriginalColor * (1 - ProgressTimesBy2);
                if (GetComponent<Renderer>().material.shader != OriginalMat.shader)
                {
                    GetComponent<Renderer>().material.shader = OriginalMat.shader;
                    GetComponent<Renderer>().material.mainTexture = OriginalMat.mainTexture;
                    GetComponent<Renderer>().material.SetTexture("_BumpMap", null);
                }
            }


            /*if (ProgressTimesBy2 > 1)
                GetComponent<Renderer>().material.color = Color.white * (ProgressTimesBy2 - 1);
            else if (ProgressTimesBy2 > 0)
            {
                GetComponent<Renderer>().material.color = OriginalColor * (1 - ProgressTimesBy2);
                if (GetComponent<Renderer>().material.shader != OriginalMat.shader)
                {
                    GetComponent<Renderer>().material.shader = OriginalMat.shader;
                    GetComponent<Renderer>().material.mainTexture = OriginalMat.mainTexture;
                    GetComponent<Renderer>().material.SetTexture("_BumpMap", null);
                }
            }*/
        }
        void Cloaking(float ProgressTimesBy2, Color OriginalColor, Vector2 BumpMapOffSet)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (ProgressTimesBy2 < 1) 
                renderer.material.color = renderer.material.color * (1 - ProgressTimesBy2);
            else if (ProgressTimesBy2 < 1.5f)
            {
                renderer.material.color = Color.black * (1.5f - ProgressTimesBy2) * 4;
                if (renderer.material.shader != InvisibleMat.shader)
                {
                    renderer.material.shader = InvisibleMat.shader;
                    renderer.material.mainTexture = InvisibleMat.mainTexture;
                    renderer.material.SetTexture("_BumpMap", InvisibleBumpedMat.mainTexture);
                }
            }
            else if (ProgressTimesBy2 < 2f)
            {
                renderer.material.SetColor("_BumpMap", Color.black * (1.5f - ProgressTimesBy2) * 4);
                if (renderer.material.shader != InvisibleBumpedMat.shader)
                {
                    renderer.material.shader = InvisibleBumpedMat.shader;
                    renderer.material.mainTexture = null;
                    renderer.material.SetTexture("_BumpMap", InvisibleBumpedMat.GetTexture("_BumpMap"));
                }
            }
            renderer.material.SetTextureOffset("_BumpMap", BumpMapOffSet);
        }
    }
    public class StealthCloakFieldScript : MonoBehaviour
    {
        public bool useFlashVFX = false;
        protected float cloakTimer;
        private bool flashing = false;

        public void setCloakTimer(float time)
        {
            cloakTimer = time;
        }
        // For host/normal game play
        protected PROCloakScript myBeloning;
        protected static Dictionary<System.Guid, StealthCloakFieldScript> dicForSCFS = new Dictionary<System.Guid, StealthCloakFieldScript>();

        public PROCloakScript MyBeloning
        {
            get { return myBeloning; }
            set
            {
                myBeloning = value;
            }
        }

        public bool Flashing
        {
            get
            {
                return flashing;
            }
            set
            {
                flashing = value;
            }
        }

        public static void PushInNewSCFS(Block cloakBlock, StealthCloakFieldScript instancee)
        {
            try
            {
                StealthCloakFieldScript blockk = dicForSCFS[cloakBlock.Guid];
                Destroy(blockk.gameObject);
            }
            catch
            {

            }
            dicForSCFS[cloakBlock.Guid] = instancee;
        }
        public void Update()
        {
            // For destorying this instance in order to prevent bugs
            //if (myBeloning == null && (!StatMaster.isMP && StatMaster.) {
            //    DestroyImmediate(this.gameObject);
            //    ConsoleController.ShowMessage("DestoryWay1");
            //}
            if (!Game.IsSimulatingGlobal && !Game.IsSimulatingLocal && !Game.IsSimulating && !StatMaster.isMP) {
                dicForSCFS.Remove(myBeloning.)
                DestroyImmediate(this.gameObject);
            }
        }
        public void OnTriggerStay(Collider col)
        {
            GameObject go = col.gameObject.GetComponentInParent<Rigidbody>().gameObject;
            foreach (BeingPROCloakedScript cloakingObject in go.GetComponentsInChildren<BeingPROCloakedScript>())
            {
                try
                {
                    if (cloakingObject != null)
                        if (useFlashVFX && flashing)
                        {
                            cloakingObject.Flash();
                        }
                        else
                            cloakingObject.InsideAField = true;
                }
                catch
                {
                }
            }
        }
    }

    public class StealthCloakFieldScriptMP : StealthCloakFieldScript
    {
        // For mp client gameplay
        public static StealthCloakFieldScript getSCFS(Block cloakBlock)
        {
            return (StealthCloakFieldScript)dicForSCFS[cloakBlock.Guid];
        }
        public static void delSCFS(Block cloakBlock)
        {
            dicForSCFS.Remove(cloakBlock.Guid);
        }
        public void CloakSizeModifier(float size, bool Activated, float ReactivationTimer, Vector3 pos, bool flashish)
        {
            if (Activated || ReactivationTimer < cloakTimer)
            {
                this.transform.position = pos;
                this.transform.localScale = Vector3.one * size;
            }
            else
            {
                this.transform.position = new Vector3(0, 999, 0) * -1000;
            }
            Flashing = flashish;
        }
        public void FixedUpdate()
        {
            try
            {
                Update();
                Collider[] cols = Physics.OverlapSphere(this.transform.position, this.transform.localScale.x);
                foreach (Collider col in cols)
                {
                    BeingPROCloakedScript cloakingObject = col.gameObject.GetComponent<BeingPROCloakedScript>();
                    if (cloakingObject != null)
                        if (useFlashVFX && Flashing)
                        {
                            cloakingObject.Flash();
                        }
                        else cloakingObject.InsideAField = true;
                }
            }
            catch { }
        }
    }
}
