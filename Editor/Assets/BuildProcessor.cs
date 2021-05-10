using UnityEngine;
using System;

namespace BuildFrontend
{
    public abstract class BuildProcessor: ScriptableObject
    {
        public abstract bool OnPreProcess(BuildTemplate template);
        public abstract bool OnPostProcess(BuildTemplate template);
    }

    public class BuildProcessorException: Exception
    {
        public BuildProcessor processor;
        public BuildTemplate template;
        public BuildProcessorException(BuildProcessor p, BuildTemplate t)
        {
            processor = p;
            template = t;
        }
    }
}


