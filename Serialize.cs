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
    internal static class Serialize
    {
        internal static string SerializeIntList(List<int> list) => string.Join(",", list.Select(x => x.ToString()).ToArray());

        internal static XElement SerializeKeyframe(Keyframe key, int tangentMode)
        {
            XElement elem = new XElement("Keyframe");
            elem.SetAttributeValue("time", key.time);
            elem.SetAttributeValue("value", key.value);
            elem.SetAttributeValue("inTangent", key.inTangent);
            elem.SetAttributeValue("outTangent", key.outTangent);
            elem.SetAttributeValue("tangentMode", tangentMode);
            return elem;
        }

        internal static XElement SerializeCurve(MovieCurve curve)
        {
            XElement elem = new XElement("MovieCurve",
                                         curve.keyframes.Select((key,i) => SerializeKeyframe(key, curve.GetTangentMode(i))));


            elem.SetAttributeValue("name", curve.name);
            elem.SetAttributeValue("preWrapMode", curve.curve.preWrapMode);
            elem.SetAttributeValue("postWrapMode", curve.curve.postWrapMode);

            return elem;
        }

        internal static XElement SerializeCurveClip(MovieCurveClip clip)
        {
            XElement elem = new XElement("MovieCurveClip",
                                         from curve in clip.curves
                                         select SerializeCurve(curve));


            elem.SetAttributeValue("frame", clip.frame);
            elem.SetAttributeValue("length", clip.length);

            return elem;
        }

        internal static XElement SerializeCurveTrackClips(MovieTrack track)
        {
            XElement elem = new XElement("Clips",
                                         from clip in track.clips
                                         select SerializeCurveClip(clip));

            return elem;
        }

        internal static XElement SerializeCameraTargetTrack(MovieCameraTargetTrack track) =>
            new XElement("MovieCameraTargetTrack",
                         SerializeCurveTrackClips(track));

        internal static XElement SerializeMaidAnimationTrack(MovieMaidAnimationTrack track)
        {
            XElement elem = new XElement("MovieMaidAnimationTrack", SerializeCurveTrackClips(track));
            elem.SetAttributeValue("maidGuid", track.maid.Param.status.guid);
            elem.SetAttributeValue("animId", track.animation.id);

            return elem;
        }

        internal static XElement SerializeMaidFaceTrack(MovieMaidFaceTrack track)
        {
            XElement elem = new XElement("MovieMaidFaceTrack", SerializeCurveTrackClips(track));

            elem.SetAttributeValue("maidGuid", track.maid.Param.status.guid);

            return elem;
        }

        internal static XElement SerializeMovieProperty(MovieProperty prop, List<int> curveIndexes)
        {
            XElement elem = new XElement("Property");

            if(prop.IsField())
            {
                elem.SetAttributeValue("type", "field");
            }
            else
            {
                elem.SetAttributeValue("type", "property");
            }
            elem.SetAttributeValue("name", prop.Name);
            elem.SetAttributeValue("curveIndexes", SerializeIntList(curveIndexes));

            return elem;
        }

        internal static XElement SerializeMovieProperties(MoviePropertyTrack track) =>
            new XElement("Properties",
                         track.propsToChange.Select((prop, i) => SerializeMovieProperty(prop, track.GetCurveIdxesForProp(i)))
                         );

        internal static XElement SerializePropertyTrack(MoviePropertyTrack track)
        {
            XElement elem = new XElement("MoviePropertyTrack", SerializeCurveTrackClips(track),
                                         SerializeMovieProperties(track));

            elem.SetAttributeValue("type", track.targetType);
            elem.SetAttributeValue("objectTag", track.target.tag);
            elem.SetAttributeValue("objectName", track.target.name);
            elem.SetAttributeValue("componentType", track.component.GetType());

            return elem;
        }

        internal static XElement SerializeTrack(MovieTrack track)
        {
            XElement elem = new XElement("ERROR");

            if(track is MovieCameraTargetTrack)
                elem = SerializeCameraTargetTrack((MovieCameraTargetTrack)track);
            else if(track is MovieMaidAnimationTrack)
                elem = SerializeMaidAnimationTrack((MovieMaidAnimationTrack)track);
            else if(track is MovieMaidFaceTrack)
                elem = SerializeMaidFaceTrack((MovieMaidFaceTrack)track);
            else if(track is MoviePropertyTrack)
                elem = SerializePropertyTrack((MoviePropertyTrack)track);

            return elem;
        }

        internal static XElement SerializeTracks(List<MovieTrack> tracks)
            => new XElement("Tracks",
                            from track in tracks
                            select SerializeTrack(track));

        internal static XDocument SerializeTake(TimelineWindow win)
            => new XDocument(new XElement("Take",
                                          SerializeTracks(win.tracks)));
    }
}
