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
    internal static class Deserialize
    {
        internal static string GetSavePath(string saveName) => "";

        internal static List<string> GetMaidGuids(XDocument doc) =>
            doc.Descendants()
            .Where(desc => desc.Attribute("maidGuid") != null)
            .Select(desc => desc.Attribute("maidGuid").Value)
            .ToList();

        internal static float GetFloatAttr(XElement element, string name)
        {
            float fVal = 0f;
            if(float.TryParse(element.Attribute(name).Value, out float fTmp))
                fVal = fTmp;
            return fVal;
        }

        internal static int GetIntAttr(XElement element, string name)
        {
            int iVal = 0;
            if(int.TryParse(element.Attribute(name).Value, out int iTmp))
                iVal = iTmp;
            return iVal;
        }

        internal static Type GetTypeAttr(XElement element, string name)
        {
            try
            {
                string typeName = element.Attribute(name).Value;
                return typeof(UnityEngine.GameObject).Assembly.GetType(typeName);
            }
            catch(Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }

        internal static List<int> GetIntListAttr(XElement element, string name) =>
            element.Attribute(name).Value
            .Split(',')
            .Select(Int32.Parse)
            .ToList();

        internal static Maid FindMaid(string maidGuid)
        {
            var results = GameMain.Instance.CharacterMgr.GetStockMaidList()
                .Select((m, i) => new { M=m, I=i })
                .Where(v => v.M.Param.status.guid == maidGuid)
                .Select(v => v.M)
                .ToList();

            if(results.Count == 0)
                return null;

            Maid maid = results.First();

            // Maid maid = GameMain.Instance.CharacterMgr.Activate(index, index, false, false);
            // maid = GameMain.Instance.CharacterMgr.CharaVisible(index, true, false);

            return maid;
        }

        internal static Keyframe DeserializeKeyframe(XElement elem)
        {
            Keyframe key = new Keyframe();

            key.time = GetFloatAttr(elem, "time");
            key.value = GetFloatAttr(elem, "value");
            key.inTangent = GetFloatAttr(elem, "inTangent");
            key.outTangent = GetFloatAttr(elem, "outTangent");

            return key;
        }

        internal static MovieCurve DeserializeCurve(XElement elem)
        {
            MovieCurve curve = new MovieCurve();

            var keys = from e in elem.Elements()
                select DeserializeKeyframe(e);

            curve.name = elem.Attribute("name").Value;
            curve.tangentModes = elem.Elements().Select(e => GetIntAttr(e, "tangentMode")).ToList();

            AnimationCurve animCurve = new AnimationCurve(keys.ToArray());
            animCurve.preWrapMode = (WrapMode)GetIntAttr(elem, "preWrapMode");
            animCurve.postWrapMode = (WrapMode)GetIntAttr(elem, "postWrapMode");
            curve.curve = animCurve;

            return curve;
        }

        internal static MovieCurveClip DeserializeCurveClip(XElement elem)
        {
            MovieCurveClip clip = new MovieCurveClip();

            var curves = from e in elem.Elements()
                select DeserializeCurve(e);

            clip.curves = curves.ToList();
            clip.frame = GetIntAttr(elem, "frame");
            clip.length = GetIntAttr(elem, "length");

            clip.RemakeTexture();

            return clip;
        }

        internal static List<MovieCurveClip> DeserializeCurveClips(XElement elem) =>
            (from e in elem.Element("Clips").Elements()
             select DeserializeCurveClip(e))
            .ToList();

        internal static MovieCameraTargetTrack DeserializeCameraTargetTrack(XElement elem)
        {
            MovieCameraTargetTrack track = new MovieCameraTargetTrack();

            track.clips = DeserializeCurveClips(elem);

            return track;
        }

        internal static MovieMaidAnimationTrack DeserializeMaidAnimationTrack(XElement elem)
        {
            try
            {
                String maidGuid = elem.Attribute("maidGuid").Value;
                int animationId = GetIntAttr(elem, "animId");

                Maid maid = FindMaid(maidGuid);
                if(maid == null)
                    throw new ArgumentNullException($"Failed to find maid with GUID {maidGuid}!");

                MovieMaidAnimationTrack track = new MovieMaidAnimationTrack(maid, animationId);
                track.clips = DeserializeCurveClips(elem);

                return track;
            }
            catch(Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }

        internal static MovieMaidFaceTrack DeserializeMaidFaceTrack(XElement elem)
        {
            try
            {
                String maidGuid = elem.Attribute("maidGuid").Value;

                Maid maid = FindMaid(maidGuid);
                if(maid == null)
                    throw new ArgumentNullException($"Failed to find maid with GUID {maidGuid}!");

                MovieMaidFaceTrack track = new MovieMaidFaceTrack(maid);
                track.clips = DeserializeCurveClips(elem);
                return track;
            }
            catch(Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }

        internal static MovieProperty DeserializeMovieProperty(XElement elem, Type componentType)
        {
            bool isField = elem.Attribute("type").Value == "field";
            string propName = elem.Attribute("name").Value;

            MovieProperty prop = null;
            if(isField)
            {
                FieldInfo fi = componentType.GetField(propName);
                prop = new MovieProperty(fi);
            }
            else
            {
                PropertyInfo pi = componentType.GetProperty(propName);
                prop = new MovieProperty(pi);
            }

            return prop;
        }

        internal static List<MovieProperty> DeserializeMovieProperties(XElement elem, Type componentType) =>
            (from e in elem.Element("Properties").Elements()
             select DeserializeMovieProperty(e, componentType))
            .ToList();

        internal static MoviePropertyTrack DeserializePropertyTrack(XElement elem)
        {
            try
            {
                MoviePropertyTrack.ObjectType objectType = (MoviePropertyTrack.ObjectType)GetIntAttr(elem, "type");
                string objectTag = elem.Attribute("objectTag").Value;
                string objectName = elem.Attribute("objectName").Value;
                GameObject obj = GameObject.Find(objectName);

                if(obj == null)
                    throw new ArgumentNullException($"No GameObject named {objectName} found!");

                Type componentType = GetTypeAttr(elem, "componentType");
                Component compo = obj.GetComponent(componentType);
                if(compo == null)
                    throw new ArgumentNullException($"No Component of type {componentType.Name} in GameObject {objectName} found!");
                var curveIdxes = elem.Element("Properties").Elements()
                    .Select(e => GetIntListAttr(e, "curveIndexes"))
                    .ToList();

                Dictionary<int, List<int>> propIdxToCurveIdxes = new Dictionary<int, List<int>>();

                for(int i = 0; i < curveIdxes.Count; i++)
                {
                    propIdxToCurveIdxes[i] = curveIdxes[i];
                }

                Debug.Log("Load: " + obj + " " + compo);
                MoviePropertyTrack track = new MoviePropertyTrack(obj, compo);
                track.clips = DeserializeCurveClips(elem);
                track.propsToChange = DeserializeMovieProperties(elem, componentType);
                track.propIdxToCurveIdxes = propIdxToCurveIdxes;

                return track;
            }
            catch(Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }

        internal static MovieTrack DeserializeTrack(XElement elem)
        {
            MovieTrack track = null;
            string name = elem.Name.ToString();
            if(name == "MovieCameraTargetTrack")
                track = DeserializeCameraTargetTrack(elem);
            else if(name == "MovieMaidAnimationTrack")
                track = DeserializeMaidAnimationTrack(elem);
            else if(name == "MovieMaidFaceTrack")
                track = DeserializeMaidFaceTrack(elem);
            else if(name == "MoviePropertyTrack")
                track = DeserializePropertyTrack(elem);

            return track;
        }

        internal static List<MovieTrack> DeserializeTracks(XElement elem) =>
            (from e in elem.Element("Tracks").Elements()
             select DeserializeTrack(e))
            .ToList();

        internal static void DeserializeEnvironment(XElement elem)
        {
            string bgVal = elem.Element("Environment").Attribute("background").Value;

            // changing the background even though it's the same seems to cause a race condition?
            if(GameMain.Instance.BgMgr.GetBGName() != bgVal)
                GameMain.Instance.BgMgr.ChangeBg(bgVal);
        }

        internal static MovieTake DeserializeTake(XDocument doc)
        {
            XElement elem = doc.Element("Take");

            DeserializeEnvironment(elem);

            MovieTake take = new MovieTake();
            take.tracks = DeserializeTracks(elem);

            return take;
        }
    }
}
