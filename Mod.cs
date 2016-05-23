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
        public override Version Version { get { return new Version("1.4"); } }
        public override string Name { get { return "Cloaking_Device_Mod"; } }
        public override string DisplayName { get { return "Cloaking Device Mod"; } }
        public override string BesiegeVersion { get { return "v0.27"; } }
        public override string Author { get { return "覅是"; } }
        protected Block CloakDevice = new Block()
            ///模块ID
            .ID(528)

            ///模块名称
            .BlockName("Cloaking Device I​")

            ///模型信息
            .Obj(new List<Obj> { new Obj("turret.obj", //Obj
                                         "Cormack\'s Modified Tracking Computer​.png", //贴图
                                         new VisualOffset(new Vector3(0.01f, 0.01f, 0.01f), //Scale
                                                          new Vector3(0f, 0f, 0f), //Position
                                                          new Vector3(0f, 0f, 0f)))//Rotation
            })

            ///在UI下方的选模块时的模样
            .IconOffset(new Icon(new Vector3(1.30f, 1.30f, 1.30f) * 0.06f,  //Scale
                                 new Vector3(-0.11f, -0.13f, 0.00f),  //Position
                                 new Vector3(85f, 90f, 270f))) //Rotation

            ///没啥好说的。
            .Components(new Type[] {
                                    typeof(CloakScript),
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
            .ShowCollider(true)

            ///碰撞器
            .CompoundCollider(new List<ColliderComposite> {
                ColliderComposite.Box(new Vector3(0.7f, 0.7f, 1.3f), new Vector3(0f, 0f, 0.8f), new Vector3(0f, 0f, 0f)),
                ColliderComposite.Capsule(0.35f,1.0f,Direction.Z,new Vector3(0,0,0.8f),Vector3.zero),/*
                                ColliderComposite.Sphere(0.49f,                                //radius
                                                         new Vector3(-0.10f, -0.05f, 0.27f),   //Position
                                                         new Vector3(0f, 0f, 0f))              //Rotation
                                                         .IgnoreForGhost(),                    //Do not use this collider on the ghost

                                ColliderComposite.Capsule(0.33f,                               //radius
                                                          1.33f,                               //length
                                                          Direction.Y,                         //direction
                                                          new Vector3(-0.52f, 0.38f, 0.30f),   //position
                                                          new Vector3(5f, 0f, -5f)),           //rotation                                
                                
                                ColliderComposite.Box(new Vector3(0.65f, 0.65f, 0.25f),        //scale
                                                      new Vector3(0f, 0f, 0.25f),              //position
                                                      new Vector3(0f, 0f, 0f)),                //rotation
                                
                                ColliderComposite.Sphere(0.5f,                                  //radius
                                                         new Vector3(-0.10f, -0.05f, 0.35f),    //Position
                                                         new Vector3(0f, 0f, 0f))               //Rotation
                                                         .Trigger().Layer(2)
                                                         .IgnoreForGhost(),                     //Do not use this collider on the ghost
                              //ColliderComposite.Box(new Vector3(0.35f, 0.35f, 0.15f), new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f)).Trigger().Layer(2).IgnoreForGhost(),   <---Example: Box Trigger on specific Layer*/
            })

            ///你的模块是不是可以忽视强搭
            .IgnoreIntersectionForBase()

            ///载入资源
            .NeededResources(new List<NeededResource> {
                                new NeededResource(ResourceType.Audio,"Cloaking.ogg"),
                                new NeededResource(ResourceType.Audio,"Decloaking.ogg"),
                                new NeededResource(ResourceType.Texture,"Speed_Bump.png")
            })

            ///连接点
            .AddingPoints(new List<AddingPoint> {
                               (AddingPoint)new BasePoint(true, true)         //底部连接点。第一个是指你能不能将其他模块安在该模块底部。第二个是指这个点是否是在开局时粘连其他链接点
                                                .Motionable(true,true,true) //底点在X，Y，Z轴上是否是能够活动的。
                                                .SetStickyRadius(0.5f),        //粘连距离
                              new AddingPoint(new Vector3(0f, 0f, 1f), new Vector3(-180f, 00f, 360f),true).SetStickyRadius(0.3f),
                              new AddingPoint(new Vector3(0f, 0f, 1f), new Vector3(-90f, 00f, 90f),true).SetStickyRadius(0.3f),
                              new AddingPoint(new Vector3(0f, 0f, 1f), new Vector3(180f, 00f, 180f),true).SetStickyRadius(0.3f),
                              new AddingPoint(new Vector3(0f, 0f, 1f), new Vector3(90f, 00f, 270f),true).SetStickyRadius(0.3f),


                              new AddingPoint(new Vector3(0f, 0f, 1f), new Vector3(0f, -90f, 90f),true).SetStickyRadius(0.3f),
            });

        public override void OnLoad()
        {
            LoadBlock(CloakDevice);//加载该模块
        }
        public override void OnUnload() { }
    }

    public class CloakScript : BlockScript
    {
        protected MKey Key1;

        private AudioSource CloakAudio;
        private AudioSource DecloakAudio;
        private float size;
        private float FieldSize;
        private bool Activated;
        private float progress;
        private float ReactivationTimer;
        private Material InvTex;
        private float CoolDownTime;
        public float 记录器 = 0;

        public override void SafeAwake()
        {
            Key1 = AddKey("Activation", //按键信息
                                 "ACT",           //名字
                                 KeyCode.P);       //默认按键
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
            FieldSize = 10;
            CoolDownTime = 2;
            InvTex = new Material(Shader.Find("FX/Glass/Stained BumpDistort"));
            //InvTex = new Material(Shader.Find("Transparent/Diffuse"));
            InvTex.color = new Color(1, 1, 1, 0);
            InvTex.mainTexture = resources["Speed_Bump.png"].texture;
            InvTex.SetTexture("_BumpMap", resources["Speed_Bump.png"].texture);
            foreach (MeshRenderer REDer in Machine.Active().SimulationMachine.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!REDer.GetComponent<BeingCloakedScript>())
                    {
                    REDer.gameObject.AddComponent<BeingCloakedScript>();
                    REDer.gameObject.GetComponent<BeingCloakedScript>().InvisibleMat = (InvTex);
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
            }
        }

        protected override void OnSimulateFixedUpdate()
        {
            ReactivationTimer += Time.fixedDeltaTime;


            if (Activated)
            {
                CloakAudio.Play();
                size = Math.Max(Math.Max(this.transform.localScale.y, this.transform.localScale.z), this.transform.localScale.x) * FieldSize * Mathf.Min(ReactivationTimer, 3) / 3;
            }
            else {
                size = FieldSize - Math.Max(
                    Math.Max(this.transform.localScale.y,this.transform.localScale.z),
                    this.transform.localScale.x)
                    * FieldSize * Mathf.Min(ReactivationTimer,3) / 3;
            }

            foreach (BeingCloakedScript BCS in FindObjectsOfType<BeingCloakedScript>())
            {
                if ((BCS.MyPos - this.transform.position).sqrMagnitude < size * size)
                {
                    progress += Time.fixedDeltaTime / 3;
                    BCS.InsideAField = true;
                }
            }
            //Debug.Log(Activated + " " + size);
        }
    }
    public class BeingCloakedScript:MonoBehaviour
    {
        private Material OriginalMat;
        public Material InvisibleMat;
        public Vector3 MyPos;
        public bool InsideAField = false;
        public float 渐变数值;
        void Start()
        {
            MyPos = this.transform.position;
            渐变数值 = 0;
            InsideAField = false;
            OriginalMat = new Material(Material.Instantiate(GetComponent<Renderer>().material));
            GetComponent<Renderer>().material.color = Color.white;
            //GetComponent<Renderer>().material.mainTexture = InvisibleMat.mainTexture;
            //GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0);
            DontDestroyOnLoad(InvisibleMat);
            DontDestroyOnLoad(OriginalMat);
        }
        void FixedUpdate()
        {
            MyPos = this.transform.position;
            if (InsideAField) 渐变数值 += Time.fixedDeltaTime / 3;
            if (!InsideAField) 渐变数值 -= Time.fixedDeltaTime / 3;
            渐变数值 = Mathf.Clamp01(渐变数值);
            if(InsideAField)
            {
                Cloaking(渐变数值 * 2);
            }
            else
            {
                Decloaking(渐变数值 * 2);
            }
            InsideAField = false;
        }
        void Decloaking(float ProgressTimesBy2)
        {
            ProgressTimesBy2 = Mathf.Clamp(ProgressTimesBy2, 0, 2);
            if (ProgressTimesBy2 > 1)
                GetComponent<Renderer>().material.color = new Color(ProgressTimesBy2 - 1, ProgressTimesBy2 - 1, ProgressTimesBy2 - 1, ProgressTimesBy2 - 1);
            else if (ProgressTimesBy2 > 0)
            {
                GetComponent<Renderer>().material.color = new Color(1 - ProgressTimesBy2, 1 - ProgressTimesBy2, 1 - ProgressTimesBy2, 1 - ProgressTimesBy2);
                if (GetComponent<Renderer>().material.shader != OriginalMat.shader)
                {
                    GetComponent<Renderer>().material.shader = OriginalMat.shader;
                    GetComponent<Renderer>().material.mainTexture = OriginalMat.mainTexture;
                    GetComponent<Renderer>().material.SetTexture("_BumpMap", null);
                }
            }
        }
        void Cloaking(float ProgressTimesBy2)
        {
            if (ProgressTimesBy2 < 1)
                GetComponent<Renderer>().material.color = new Color( 1 - ProgressTimesBy2, 1 - ProgressTimesBy2, 1 - ProgressTimesBy2, 1 - ProgressTimesBy2);
            else if (ProgressTimesBy2 < 2)
            {
                GetComponent<Renderer>().material.color = new Color(ProgressTimesBy2 - 1,ProgressTimesBy2 - 1,ProgressTimesBy2 - 1,ProgressTimesBy2 - 1);
                if(GetComponent<Renderer>().material.shader != InvisibleMat.shader)
                {
                    GetComponent<Renderer>().material.shader = InvisibleMat.shader;
                    GetComponent<Renderer>().material.mainTexture = null;
                    GetComponent<Renderer>().material.SetTexture("_BumpMap", InvisibleMat.mainTexture);
                }
            }

        }
    }
    public class CloakFieldScript : MonoBehaviour
    {
        public Queue<List<float>> GeneratorPositions = new Queue<List<float>>();
    }
    //Physics stuff
}


