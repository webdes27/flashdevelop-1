﻿namespace ProjectManager.Projects.AS2
{
    public class AS2MovieOptions: MovieOptions
    {
        public AS2MovieOptions()
        {
            Language = "as2";
            MajorVersion = 9;
            Platform = TargetPlatforms[0];
        }

        public override OutputType[] OutputTypes =>
            new[] { OutputType.OtherIDE, OutputType.CustomBuild, OutputType.Application };

        public override OutputType DefaultOutput(string platform) => OutputType.Application;
    }
}