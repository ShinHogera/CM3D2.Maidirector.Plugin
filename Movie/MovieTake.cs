using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;

namespace CM3D2.Maidirector.Plugin
{
    public class MovieTake
    {
        public List<MovieTrack> tracks;

        public MovieTake()
        {
           this.tracks = new List<MovieTrack>();
        }

        public int GetEndFrame() => (int)(this.tracks.OrderByDescending(track => track.endTime).First().endTime + 300);
    }
}
