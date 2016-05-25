using System;
using System.Collections;
using System.Collections.Generic;
using spaar.ModLoader;
using TheGuysYouDespise;
using UnityEngine;

namespace Blocks
{
    public class CloakingDeviceMod : BlockMod
    {
        public override Version Version { get { return new Version("2.0"); } }
        public override string Name { get { return "Cloaking_Device_Mod"; } }
        public override string DisplayName { get { return "Cloaking Device Mod"; } }
        public override string BesiegeVersion { get { return "v0.27"; } }
        public override string Author { get { return "覅是"; } }
        protected Block ImprovedCloakDevice = new Block()
            ///模块ID
            .ID(528)

            ///模块名称
            .BlockName("Improved Cloaking Device​")

            ///模型信息
            .Obj(new List<Obj> { new Obj("Cloak Device.obj", //Obj
                                         "Cloak Device Texture.png", //贴图
                                         new VisualOffset(new Vector3(1f, 1f, 1f), //Scale
                                                          new Vector3(0f, 0f, 0f), //Position
                                                          new Vector3(0f, 0f, 0f)))//Rotation
            })

            ///在UI下方的选模块时的模样
            .IconOffset(new Icon(new Vector3(1.30f, 1.30f, 1.30f),  //Scale
                                 new Vector3(-0.11f, -0.13f, 0.00f),  //Position
                                 new Vector3(85f, 90f, 270f))) //Rotation

            ///没啥好说的。
            .Components(new Type[] {
                                    typeof(IMPCloakScript),
            })

            ///给搜索用的关键词
            .Properties(new BlockProperties().SearchKeywords(new string[] {
                                                             "Cloak",
                                                             "隐身",
                                                             "invisible"
                                             })
            )
            ///质量
            .Mass(2f)

            ///是否显示碰撞器（在公开你的模块的时候记得写false）
            .ShowCollider(false)

            ///碰撞器
            .CompoundCollider(new List<ColliderComposite> {
                ColliderComposite.Mesh("Cloak Device.obj",Vector3.one,Vector3.zero,new Vector3(90,0,0))
                                })

            ///你的模块是不是可以忽视强搭
            .IgnoreIntersectionForBase()

            ///载入资源
            .NeededResources(new List<NeededResource> {
                                new NeededResource(ResourceType.Audio,"Cloaking.ogg"),
                                new NeededResource(ResourceType.Audio,"Decloaking.ogg"),
                                new NeededResource(ResourceType.Texture,"Cloak disturb.png"),
                                new NeededResource(ResourceType.Texture,"ALL BLACK.jpg")
            })

            ///连接点
            .AddingPoints(new List<AddingPoint> {
                               (AddingPoint)new BasePoint(true, true)         //底部连接点。第一个是指你能不能将其他模块安在该模块底部。第二个是指这个点是否是在开局时粘连其他链接点
                                                .Motionable(false,false,false) //底点在X，Y，Z轴上是否是能够活动的。
                                                .SetStickyRadius(0.5f),        //粘连距离
            });

        public override void OnLoad()
        {
            LoadBlock(ImprovedCloakDevice);//加载该模块
        }
        public override void OnUnload() { }
    }

    public class IMPCloakScript : BlockScript
    {
        protected MKey Key1;
        protected MSlider Speed;
        protected MSlider Range;
        protected MSlider Cooldown;

        private AudioSource CloakAudio;
        private AudioSource DecloakAudio;
        private float size;
        private float FieldSize;
        private bool Activated;
        private float progress;
        private float ReactivationTimer;
        private Material InvBumpTex;
        private Material InvTex;
        private float CoolDownTime;
        public float 记录器 = 0;
        public GameObject FieldDetector;

        public override void SafeAwake()
        {
            Key1 = AddKey("Activation", //按键信息
                                 "ACT",           //名字
                                 KeyCode.P);       //默认按键

            Speed = AddSlider("Time for changing", "TFC", 3, 0.1f, 10);
            Range = AddSlider("Size Of Field", "Size",10, 1, 80);
            Cooldown = AddSlider("Duration", "DUR", 5, 1, 10);
        }

        protected virtual IEnumerator UpdateMapper()
        {
            if (BlockMapper.CurrentInstance == null)
                yield break;
            while (Input.GetMouseButton(0))
                yield return null;
            BlockMapper.CurrentInstance.Copy();
            BlockMapper.CurrentInstance.Paste();
            yield break;
        }

        public override void OnSave(XDataHolder data)
        {
            SaveMapperValues(data);
        }
        public override void OnLoad(XDataHolder data)
        {
            LoadMapperValues(data);
            if (data.WasSimulationStarted) return;
        }
        protected override void OnSimulateStart()
        {
            ReactivationTimer = 5;
            CloakAudio = this.gameObject.AddComponent<AudioSource>();
            CloakAudio.clip = resources["Cloaking.ogg"].audioClip;
            CloakAudio.loop = false;
            CloakAudio.volume = 1f;
            DecloakAudio = this.gameObject.AddComponent<AudioSource>();
            DecloakAudio.clip = resources["Decloaking.ogg"].audioClip;
            DecloakAudio.loop = false;
            DecloakAudio.volume = 1f;
            FieldSize = Range.Value;
            CoolDownTime = Cooldown.Value;
            InvBumpTex = new Material(Shader.Find("FX/Glass/Stained BumpDistort"));
            
            InvBumpTex.color = new Color(1, 1, 1, 0);
            InvBumpTex.mainTexture = resources["Cloak disturb.png"].texture;
            InvBumpTex.SetTexture("_BumpMap", resources["Cloak disturb.png"].texture);

            InvTex = new Material(Shader.Find("Transparent/Diffuse"));
            InvTex.color = new Color(1, 1, 1, 0);
            InvTex.mainTexture = resources["ALL BLACK.jpg"].texture;

            FieldDetector = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            FieldDetector.name = "FieldDetector";
            FieldDetector.GetComponent<SphereCollider>().isTrigger = true;
            FieldDetector.transform.position = this.transform.position;
            Destroy(FieldDetector.GetComponent<Renderer>());
            FieldDetector.AddComponent<CloakFieldScript>();

            foreach (MeshRenderer REDer in Machine.Active().SimulationMachine.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!REDer.GetComponent<BeingCloakedScript>())
                    {
                    REDer.gameObject.AddComponent<BeingCloakedScript>();
                    REDer.gameObject.GetComponent<BeingCloakedScript>().InvisibleBumpedMat = (InvBumpTex);
                    REDer.gameObject.GetComponent<BeingCloakedScript>().InvisibleBumpedMat.mainTexture = (InvBumpTex.mainTexture);
                    REDer.gameObject.GetComponent<BeingCloakedScript>().InvisibleMat = (InvTex);
                    REDer.gameObject.GetComponent<BeingCloakedScript>().InvisibleMat.mainTexture = (InvTex.mainTexture);
                    REDer.gameObject.GetComponent<BeingCloakedScript>().duration = Speed.Value;
                    //REDer.gameObject.tag += "Cloaked";
                }
                }
            
        }
        protected override void OnSimulateUpdate()
        {
            //Trail.GetComponent<TrailRenderer>().material.color = Color.white;
            if (Key1.IsReleased && ReactivationTimer > CoolDownTime)
            {
                Activated = !Activated;
                ReactivationTimer = 0;
                if(!Activated) DecloakAudio.Play();
                else CloakAudio.Play();
            }
        }

        protected override void OnSimulateFixedUpdate()
        {
            ReactivationTimer += Time.fixedDeltaTime;
            
            if (Activated)
            {
                size =  FieldSize * Mathf.Min(ReactivationTimer, Speed.Value) / Speed.Value;
            }
            else {
                size = FieldSize - FieldSize * Mathf.Min(ReactivationTimer, Speed.Value) / Speed.Value;
            }
            if (Activated || ReactivationTimer < Speed.Value)
            {
                FieldDetector.transform.localScale = Vector3.one * size;
                FieldDetector.transform.position = this.transform.position;
                /*foreach (BeingCloakedScript BCS in FindObjectsOfType<BeingCloakedScript>())
                {
                    if ((BCS.MyPos - this.transform.position).sqrMagnitude < size * size)
                    {
                        progress += Time.fixedDeltaTime / 3;
                        BCS.InsideAField = true;
                    }
                }*/

            }
            else
            {
                FieldDetector.transform.position = Vector3.one * -10;
            }
        }
    }
    public class BeingCloakedScript:MonoBehaviour
    {
        private Material OriginalMat;
        public Material InvisibleBumpedMat;
        public Material InvisibleMat;
        private Color OrigColor = new Color();
        public float duration = 3;
        public Vector3 MyPos;
        public bool InsideAField = false;
        public float 渐变数值;
        public Vector2 BumpOFFSET;
        public Vector2 BumpOFFSET惯性;
        private string Oldname;
        void Start()
        {
            MyPos = this.transform.position;
            渐变数值 = 0;
            InsideAField = false;
            OriginalMat = new Material(Material.Instantiate(GetComponent<Renderer>().material));
            OrigColor = GetComponent<Renderer>().material.color;
            //GetComponent<Renderer>().material.color = Color.white;
            //GetComponent<Renderer>().material.mainTexture = InvisibleMat.mainTexture;
            //GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0);
            BumpOFFSET = Vector2.zero;
            BumpOFFSET惯性 = Vector2.zero;
            Oldname = this.GetComponentInParent<MachineTrackerMyId>().gameObject.name;
            DontDestroyOnLoad(InvisibleBumpedMat);
            DontDestroyOnLoad(InvisibleMat);
            DontDestroyOnLoad(OriginalMat);
        }
        void FixedUpdate()
        {
            BumpOFFSET惯性 += new Vector2(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f) * 0.001f;
            BumpOFFSET惯性 = new Vector2(Mathf.Clamp(BumpOFFSET惯性.x, -0.02f, 0.02f), Mathf.Clamp(BumpOFFSET惯性.y, -0.02f, 0.02f));
            BumpOFFSET += BumpOFFSET惯性;
            MyPos = this.transform.position;
            if (InsideAField) 渐变数值 += Time.fixedDeltaTime / duration;
            if (!InsideAField) 渐变数值 -= Time.fixedDeltaTime / duration;
            渐变数值 = Mathf.Clamp01(渐变数值);
            if(InsideAField)
            {
                Cloaking(渐变数值 * 2, OrigColor, BumpOFFSET);
                if (!this.GetComponentInParent<MachineTrackerMyId>().gameObject.name.Contains("Cloaked"))
                    this.GetComponentInParent<MachineTrackerMyId>().gameObject.name += "Cloaked";
            }
            else
            {
                Decloaking(渐变数值 * 2,OrigColor);
                if (this.GetComponentInParent<MachineTrackerMyId>().gameObject.name.Contains("Cloaked"))
                    this.GetComponentInParent<MachineTrackerMyId>().gameObject.name = Oldname;
            }
            InsideAField = false;
        }
        void Decloaking(float ProgressTimesBy2, Color OriginalColor)
        {
            ProgressTimesBy2 = Mathf.Clamp(ProgressTimesBy2, 0, 2);


            if (ProgressTimesBy2 > 1.5f)
            {
                GetComponent<Renderer>().material.SetColor("_BumpMap", Color.white * (ProgressTimesBy2 - 1.5f) * 4);
                if (GetComponent<Renderer>().material.shader != InvisibleBumpedMat.shader)
                {
                    GetComponent<Renderer>().material.shader = InvisibleBumpedMat.shader;
                    GetComponent<Renderer>().material.mainTexture = null;
                    GetComponent<Renderer>().material.SetTexture("_BumpMap", InvisibleBumpedMat.GetTexture("_BumpMap"));
                }
            }
            else if (ProgressTimesBy2 > 1f)
            {
                GetComponent<Renderer>().material.color = Color.white * (1.5f - ProgressTimesBy2) * 4;
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
            if (ProgressTimesBy2 < 1)
                GetComponent<Renderer>().material.color = OriginalColor * (1 - ProgressTimesBy2);
            else if (ProgressTimesBy2 < 1.5f)
            {
                GetComponent<Renderer>().material.color = Color.white * (1.5f - ProgressTimesBy2) * 4;
                if (GetComponent<Renderer>().material.shader != InvisibleMat.shader)
                {
                    GetComponent<Renderer>().material.shader = InvisibleMat.shader;
                    GetComponent<Renderer>().material.mainTexture = InvisibleMat.mainTexture;
                    GetComponent<Renderer>().material.SetTexture("_BumpMap", InvisibleBumpedMat.mainTexture);
                }
            }
            else if (ProgressTimesBy2 < 2f)
            {
                GetComponent<Renderer>().material.SetColor("_BumpMap", Color.white * (1.5f - ProgressTimesBy2) * 4);
                if (GetComponent<Renderer>().material.shader != InvisibleBumpedMat.shader)
                {
                    GetComponent<Renderer>().material.shader = InvisibleBumpedMat.shader;
                    GetComponent<Renderer>().material.mainTexture = null;
                    GetComponent<Renderer>().material.SetTexture("_BumpMap", InvisibleBumpedMat.GetTexture("_BumpMap"));
                }
            }
            GetComponent<Renderer>().material.SetTextureOffset("_BumpMap", BumpMapOffSet);
        }
    }
    public class CloakFieldScript:MonoBehaviour
    {
        void Update()
        {
            if (!AddPiece.isSimulating) Destroy(this.gameObject);
        }
        void OnTriggerStay(Collider col)
        {
            try {
                foreach (BeingCloakedScript cloakingObject in col.gameObject.GetComponentInParent<MachineTrackerMyId>().GetComponentsInChildren<BeingCloakedScript>())
                {
                    cloakingObject.InsideAField = true;
                }
            }
            catch
            {
            }
        }
    }
}


