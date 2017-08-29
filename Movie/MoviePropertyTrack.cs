using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace CM3D2.HandmaidsTale.Plugin
{
    public class MoviePropertyTrack : MovieTrack
    {
        List<MovieProperty> propsToChange;
        Dictionary<int, List<int>> propIdxToCurveIdxes;
        public GameObject target;
        public Component component;

        public string targetName;
        public ObjectType targetType;

        public enum ObjectType
        {
            Background,
            Static,
            Other
        }

        public MoviePropertyTrack(GameObject go, Component c) : base()
        {
            this.propsToChange = new List<MovieProperty>();
            this.propIdxToCurveIdxes = new Dictionary<int, List<int>>();

            this.target = go;
            this.component = c;
        }

        public void AddProp(MovieProperty movieProp)
        {
            this.propsToChange.Add(movieProp);
            this.AddNewCurves(movieProp);
        }

        public void RemoveProp(int index)
        {
            for(int h = 0; h < this.propIdxToCurveIdxes[index].Count; h++)
            {
                int i = this.propIdxToCurveIdxes[index][h];
                foreach (MovieCurveClip clip in this.clips)
                {
                    clip.RemoveCurve(i);

                    foreach (List<int> curveIdxes in this.propIdxToCurveIdxes.Values)
                    {
                        List<int> toRemove = new List<int>();
                        foreach (var elem in curveIdxes)
                        {
                            if (elem >= i)
                            {
                                toRemove.Add(elem);
                            }
                        }
                        curveIdxes.RemoveAll(x => toRemove.Contains(x));
                        toRemove = toRemove.Select(x => x - 1).ToList();
                        curveIdxes.AddRange(toRemove);
                    }
                }
            }

            this.propIdxToCurveIdxes.Remove(index);
        }

        public override void AddClipInternal(MovieCurveClip clip)
        {
            for (int i = 0; i < propsToChange.Count; i++)
            {
                MovieProperty prop = propsToChange[i];
                float[] values = prop.GetValues(this.component);

                this.AddCurvesForProp(prop, clip, i);
            }
        }

        private void AddCurvesForProp(MovieProperty prop, MovieCurveClip clip, int index)
        {
            float[] values = prop.GetValues(this.component);
            for (int j = 0; j < values.Length; j++)
            {
                int curveIdx = clip.AddCurve(new MovieCurve(clip.length, values[j], prop.Name + "." + j));
                this.propIdxToCurveIdxes[index].Add(curveIdx);
            }
        }

        private void AddNewCurves(MovieProperty prop)
        {
            for (int i = 0; i < this.propsToChange.Count; i++)
            {
                if (!this.propIdxToCurveIdxes.ContainsKey(i))
                {
                    var existing = new List<int>();
                    this.propIdxToCurveIdxes.Add(i, existing);

                    foreach (MovieCurveClip clip in this.clips)
                    {
                        AddCurvesForProp(prop, clip, i);
                    }
                }
            }
        }

        public override void PreviewTimeInternal(MovieCurveClip clip, float sampleTime)
        {
            for (int i = 0; i < propsToChange.Count; i++)
            {
                List<int> curveIdxes = this.propIdxToCurveIdxes[i];
                float[] values = curveIdxes.Select(idx => clip.curves[idx].Evaluate(sampleTime)).ToArray();
                this.propsToChange[i].SetValue(this.component, values);
            }
        }

        public override void DrawPanelExtra(float currentTime)
        {
            Rect rect = new Rect(0, 0, 25, 15);
            if (GUI.Button(rect, "+"))
            {
                GlobalPropertyPicker.Set(new Vector2(100, 100), 200, 12, this.component, (pr, fi) =>
                        {
                            if(pr == null)
                                this.AddProp(new MovieProperty(fi));
                            else
                                this.AddProp(new MovieProperty(pr));
                        });
            }
        }
    }

    public class MovieProperty
    {
        private PropertyInfo propToChange;
        private FieldInfo fieldToChange;

        public string Name
        {
            get
            {
                if(this.fieldToChange != null)
                    return this.fieldToChange.Name;

                return this.propToChange.Name;
            }
        }

        public MovieProperty(FieldInfo field)
        {
            this.fieldToChange = field;

            if (!IsSupportedType(field.FieldType))
            {
                Debug.LogError("Field " + field.Name + " not supported!");
            }
        }

        public MovieProperty(PropertyInfo prop)
        {
            this.propToChange = prop;

            if (!IsSupportedType(prop.PropertyType))
            {
                Debug.LogError("Property " + prop.Name + " not supported!");
            }
        }

        public static bool IsSupportedType(Type type)
        {
            return type.IsPrimitive || type == typeof(Vector3) || type == typeof(Color);
        }

        public void SetValue(object target, float[] values)
        {
            if(this.fieldToChange != null)
                this.SetFieldValue(target, values);
            else
                this.SetPropValue(target, values);
        }

        public float[] GetValues(object target)
        {
            if(this.fieldToChange != null)
                return this.GetFieldValues(target);
            else
                return this.GetPropValues(target);
        }

        private void SetPropValue(object target, float[] values)
        {
            try
            {
                //Debug.Log("Setting prop " + propToChange.Name + " to " + values[0]);
                Type propType = propToChange.PropertyType;
                if (propType == typeof(int))
                    propToChange.SetValue(target, (int)values[0], null);
                else if (propType == typeof(double))
                    propToChange.SetValue(target, (double)values[0], null);
                else if (propType == typeof(float))
                    propToChange.SetValue(target, values[0], null);
                else if (propType == typeof(bool))
                    propToChange.SetValue(target, values[0] > 0, null);
                else if (propType.IsEnum)
                    propToChange.SetValue(target, (int)values[0], null);
                else if (propType == typeof(Vector3))
                    propToChange.SetValue(target, new Vector3(values[0], values[1], values[2]), null);
                else if (propType == typeof(Color))
                    propToChange.SetValue(target, new Color(values[0], values[1], values[2], values[3]), null);
                else
                    Debug.LogWarning("Unsupported type " + propType);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void SetFieldValue(object target, float[] values)
        {
            try
            {
                //Debug.Log("Setting prop " + propToChange.Name + " to " + values[0]);
                Type fieldType = fieldToChange.FieldType;
                if (fieldType == typeof(int))
                    fieldToChange.SetValue(target, (int)values[0]);
                else if (fieldType == typeof(double))
                    fieldToChange.SetValue(target, (double)values[0]);
                else if (fieldType == typeof(float))
                    fieldToChange.SetValue(target, values[0]);
                else if (fieldType == typeof(bool))
                    fieldToChange.SetValue(target, values[0] > 0);
                else if (fieldType.IsEnum)
                    fieldToChange.SetValue(target, (int)values[0]);
                else if (fieldType == typeof(Vector3))
                    fieldToChange.SetValue(target, new Vector3(values[0], values[1], values[2]));
                else if (fieldType == typeof(Color))
                    fieldToChange.SetValue(target, new Color(values[0], values[1], values[2], values[3]));
                else
                    Debug.LogWarning("Unsupported type " + fieldType);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private float[] GetPropValues(object target)
        {
            Type propType = propToChange.PropertyType;
            if (propType == typeof(bool))
                return new float[] { (bool)propToChange.GetValue(target, null) ? 1 : -1 };
            else if (propType.IsEnum || propType.IsPrimitive)
                return new float[] { (float)propToChange.GetValue(target, null) };
            else if (propType == typeof(Vector3))
            {
                Vector3 vec = (Vector3)propToChange.GetValue(target, null);
                return new float[] { vec[0], vec[1], vec[2] };
            }
            else if (propType == typeof(Color))
            {
                Color c = (Color)propToChange.GetValue(target, null);
                return new float[] { c[0], c[1], c[2], c[3] };
            }
            else
            {
                Debug.LogWarning("Unsupported type " + propType);
                return new float[] { 0 };
            }
        }

        private float[] GetFieldValues(object target)
        {
            Type fieldType = fieldToChange.FieldType;
            if (fieldType == typeof(bool))
                return new float[] { (bool)fieldToChange.GetValue(target) ? 1 : -1 };
            else if (fieldType.IsEnum || fieldType.IsPrimitive)
                return new float[] { (float)fieldToChange.GetValue(target) };
            else if (fieldType == typeof(Vector3))
            {
                Vector3 vec = (Vector3)fieldToChange.GetValue(target);
                return new float[] { vec[0], vec[1], vec[2] };
            }
            else if (fieldType == typeof(Color))
            {
                Color c = (Color)fieldToChange.GetValue(target);
                return new float[] { c[0], c[1], c[2], c[3] };
            }
            else
            {
                Debug.LogWarning("Unsupported type " + fieldType);
                return new float[] { 0 };
            }
        }

        // public override XElement Save()
        // {
            
        // }

        // public override void Restore(XElement elem)
        // {
        //     ObjectType targetType = (ObjectType)Enum.Parse( typeof( ObjectType ), elem.Element("TargetType").Value.ToString());
        //     string targetName = elem.Element("TargetName").Value.ToString();

        //     GameObject target = null;
        //     Component component = null;
        //     switch(targetType)
        //     {
        //         case ObjectType.Background:
        //             GameObject background = GameMain.Instance.BgMgr.current_bg_object;
        //             target = background.transform.Find(targetName);
        //             break;
        //         case ObjectType.Static:
        //         case ObjectType.Other:
        //             // Assuming the object is always instantiated and global (camera, etc.)
        //             target = UnityEngine.GameObject.Find(targetName);
        //             break;
        //     }

        //     string componentTypeName = elem.Element("ComponentType").Value.ToString();
        //     Type componentType = typeof(UnityEngine.GameObject).Assembly.GetType(componentTypeName);
        // }
    }
}
