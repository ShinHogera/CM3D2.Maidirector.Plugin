using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityInjector;
using UnityInjector.Attributes;

namespace CM3D2.Maidirector.Plugin
{
    public class MaidLoader
    {
        private bool isLoadMaid;
        private bool isFadeOut;
        public bool enableGui = true;
        private bool isBusyInit;
        private CharacterMgr characterMgr;
        public bool IsInitted { get; private set; }

        private Maid[] maidArray;
        private List<int> selectList;
        private bool[] isAnimationStopped;
        private bool[] isPoseLocked;

        private string defaultPose;

        private int maxMaidCnt
        {
            get => this.maidArray.Length;
        }

        public delegate void onMaidsLoadedCallback();
        public onMaidsLoadedCallback onMaidsLoaded = delegate { };

        public MaidLoader()
        {
            this.characterMgr = GameMain.Instance.CharacterMgr;
            if(PhotoMotionData.data == null)
                PhotoMotionData.Create();
            this.defaultPose = PhotoMotionData.data.Where(d => !d.is_mod && !d.is_mypose && !string.IsNullOrEmpty(d.direct_file)).First().direct_file;

            this.selectList = new List<int>();
            this.selectList.Add(0);
            this.selectList.Add(1);
            this.selectList.Add(2);
            this.selectList.Add(3);
            this.selectList.Add(4);
            this.selectList.Add(5);
            this.selectList.Add(6);

            this.BuildMaidArrays();
        }

        public void SelectMaidsWithGuids(List<string> guids) =>
            this.selectList = characterMgr.GetStockMaidList()
                .Select((maid, i) => new {M=maid, I=i})
                .Where(v => guids.Contains(v.M.Param.status.guid))
                .Select(v => v.I)
                .ToList();

        private void BuildMaidArrays()
        {
            this.maidArray = new Maid[characterMgr.GetStockMaidCount()];
            this.isAnimationStopped = new bool[this.maidArray.Length];
            this.isPoseLocked = new bool[this.maidArray.Length];

            var other = GameMain.Instance.CharacterMgr.GetStockMaidList();
            for(int i = 0; i < other.Count; i++)
            {
                this.maidArray[i] = other[i];
            }
        }

        public void Init()
        {
            if(IsInitted)
                return;

            this.BuildMaidArrays();
            this.IsInitted = true;
        }

        private void ClearMaid(int index)
        {
            if (!this.isPoseLocked[index] && (this.maidArray[index] != null && this.maidArray[index].Visible))
            {
                Maid maid = this.maidArray[index];
                maid.CrossFade(this.defaultPose, false, true, false, 0.0f, 1f);
                maid.SetAutoTwistAll(true);
            }
            this.maidArray[index] = null;
            this.isAnimationStopped[index] = false;
        }

        private void ClearMaids()
        {
            for (int index = 0; index < this.maxMaidCnt; ++index)
            {
                this.ClearMaid(index);
            }
        }

        private void HideStockMaids()
        {
            for (int index = 0; index < characterMgr.GetStockMaidCount(); ++index)
                characterMgr.GetStockMaidList()[index].Visible = false;
        }


        public void StartLoad()
        {
            this.isLoadMaid = true;

            this.ClearMaids();

            // TODO: photo mode compat
            // PlacementWindow.DeActiveMaid

            this.isFadeOut = true;
            GameMain.Instance.MainCamera.FadeOut(0.0f, false, null, true);
            this.enableGui = false;

            this.HideStockMaids();
        }

        private bool IsMaidBusy(int index) => index < this.selectList.Count && this.maidArray[index] != null && this.maidArray[index].IsBusy;

        private bool CheckA(int index) => (index == this.maxMaidCnt - 1 || index < this.maxMaidCnt - 1 && this.maidArray[index + 1] == null) && this.maidArray[index] == null;

        private bool CheckB(int index) => (index != 0 || this.maidArray[index + 1] != null || this.maidArray[index] != null)
            && (index <= 0 || this.maidArray[index - 1] == null || this.maidArray[index - 1].IsBusy);

        // private bool IsEditMode() => this.sceneLevel == 5;
        private bool IsEditMode() => false;

        private void LoadMaidFull(int index)
        {
            this.maidArray[index] = GameMain.Instance.CharacterMgr.GetStockMaid((int) this.selectList[index]);
            if (!this.maidArray[index].body0.isLoadedBody)
            {
                this.maidArray[index].DutPropAll();
                this.maidArray[index].AllProcPropSeqStart();
            }
            this.maidArray[index].Visible = true;
        }

        private void LoadMaidActivate(int index, int a)
        {
            this.maidArray[index] = GameMain.Instance.CharacterMgr.Activate(a, this.selectList[index], false, false);
            this.maidArray[index] = GameMain.Instance.CharacterMgr.CharaVisible(a, true, false);
        }

        private bool TryLoadMaid(int index)
        {
            if (CheckA(index))
            {
                if (CheckB(index))
                    return false;

                if ((int) this.selectList[index] >= 12)
                {
                    this.LoadMaidFull(index);
                }
                else if (this.IsEditMode() || (index == 0 && (int) this.selectList[index] == 0))
                {
                    this.LoadMaidActivate(index, this.selectList[index]);
                }
                else if (index == 0)
                {
                    this.LoadMaidActivate(index, 0);
                }
                else if ((int) this.selectList[index] + 1 == 12)
                {
                    this.LoadMaidFull(index);
                }
                else
                {
                    this.LoadMaidActivate(index, this.selectList[index] + 1);
                }

                if (this.maidArray[index] != null && this.maidArray[index].Visible)
                {
                    this.maidArray[index].body0.boHeadToCam = true;
                    this.maidArray[index].body0.boEyeToCam = true;
                }
            }
            return true;
        }


        private void MaidUpdate()
        {
            if(this.isLoadMaid)
            {
                bool shouldLoad = !Enumerable.Range(0, this.maxMaidCnt).Any(IsMaidBusy);

                if(shouldLoad)
                {
                    for(int i = 0; i < this.selectList.Count; i++)
                    {
                        this.TryLoadMaid(i);
                    }
                }
                this.isLoadMaid = false;
            }
        }

        private bool TryProcMaid(int index1)
        {
            if (!this.isPoseLocked[index1] && this.maidArray[index1] != null)
            {
                this.maidArray[index1].CrossFade(this.defaultPose, false, true, false, 0.0f, 1f);
                this.maidArray[index1].SetAutoTwistAll(true);
            }
            /* this.poseCount[index1] = 30; */
            /* if (this.maidArray[index1] != null && this.maidArray[index1].Visible) */
            /* { */
            /*     this.maidArray[index1].body0.BoneHitHeightY = -10f; */
            /*     if (this.goSlot[(int) this.selectList[index1]] == null) */
            /*     { */
            /*         this.maidArray[index1].CrossFade(this.poseArray[0] + ".anm", false, true, false, 0.0f, 1f); */
            /*         this.maidArray[index1].SetAutoTwistAll(true); */
            /*         this.goSlot[(int) this.selectList[index1]] = new List<TBodySkin>((IEnumerable<TBodySkin>) this.maidArray[index1].body0.goSlot); */
            /*         this.bodyHit[(int) this.selectList[index1]] = new List<TBodyHit>(); */
            /*         for (int index2 = 0; index2 < this.goSlot[(int) this.selectList[index1]].Count; ++index2) */
            /*         { */
            /*             TBodyHit tbodyHit = new TBodyHit(); */
            /*             TBodyHit bodyhit = this.maidArray[index1].body0.goSlot[index2].bonehair.bodyhit; */
            /*             if (bodyhit != null) */
            /*             { */
            /*                 tbodyHit.spherelist = new List<THitSphere>((IEnumerable<THitSphere>) bodyhit.spherelist); */
            /*                 tbodyHit.m_listHandHitL = new List<THitSphere>((IEnumerable<THitSphere>) bodyhit.m_listHandHitL); */
            /*                 tbodyHit.m_listHandHitR = new List<THitSphere>((IEnumerable<THitSphere>) bodyhit.m_listHandHitR); */
            /*                 tbodyHit.RotOffset = bodyhit.RotOffset; */
            /*                 tbodyHit.tRoot = bodyhit.tRoot; */
            /*                 tbodyHit.skrt_R1 = bodyhit.skrt_R1; */
            /*                 tbodyHit.skrt_R2 = bodyhit.skrt_R2; */
            /*                 tbodyHit.skrt_R3 = bodyhit.skrt_R3; */
            /*                 tbodyHit.skrt_L1 = bodyhit.skrt_L1; */
            /*                 tbodyHit.skrt_L2 = bodyhit.skrt_L2; */
            /*                 tbodyHit.skrt_L3 = bodyhit.skrt_L3; */
            /*                 tbodyHit.MOMO_FUTO = bodyhit.MOMO_FUTO; */
            /*                 tbodyHit.HARA_FUTO = bodyhit.HARA_FUTO; */
            /*                 tbodyHit.KOSHI_SCL = bodyhit.KOSHI_SCL; */
            /*                 tbodyHit.KOSHI_SVAL = bodyhit.KOSHI_SVAL; */
            /*                 tbodyHit.BodySkinTAG = bodyhit.BodySkinTAG; */
            /*                 tbodyHit.SkirtFT = bodyhit.SkirtFT; */
            /*                 tbodyHit.MST = bodyhit.MST; */
            /*                 tbodyHit.MST_v = bodyhit.MST_v; */
            /*             } */
            /*             this.bodyHit[(int) this.selectList[index1]].Add(tbodyHit); */
            /*         } */
            /*     } */
            /* } */
            return true;
        }

        private bool IsMaidProcPropBusy(int index) => this.maidArray[index] != null && this.maidArray[index].Visible && this.maidArray[index].IsAllProcPropBusy;

        public void Update()
        {
            this.MaidUpdate();
            if(this.isFadeOut)
            {
                bool maidsFinishedLoading = !Enumerable.Range(0, this.maxMaidCnt).Any(i => IsMaidBusy(i) || IsMaidProcPropBusy(i));

                if(maidsFinishedLoading)
                {
                    if(!this.isBusyInit)
                    {
                        this.isBusyInit = true;
                    }
                    else
                    {
                        for(int i = 0; i < this.maxMaidCnt; i++)
                        {
                            this.TryProcMaid(i);
                        }
                        this.isBusyInit = false;

                        GameMain.Instance.MainCamera.FadeIn(1f, false, null, true);
                        this.isFadeOut = false;
                        this.enableGui = true;
                        this.onMaidsLoaded();
                    }
                }
            }
        }
    }
}
