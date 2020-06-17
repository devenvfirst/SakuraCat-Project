// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="IInputManager"/>.
    /// </summary>
    public static class InputManagerExtensions
    {
        /// <summary>
        /// Attempts to <see cref="IInputManager.GetSampler(string)"/> of <see cref="InputConfiguration.SubmitName"/>. 
        /// </summary>
        public static IInputSampler GetSubmit (this IInputManager mng) => mng.GetSampler(InputConfiguration.SubmitName);
        /// <summary>
        /// Attempts to <see cref="IInputManager.GetSampler(string)"/> of <see cref="InputConfiguration.CancelName"/>. 
        /// </summary>
        public static IInputSampler GetCancel (this IInputManager mng) => mng.GetSampler(InputConfiguration.CancelName);
        /// <summary>
        /// Attempts to <see cref="IInputManager.GetSampler(string)"/> of <see cref="InputConfiguration.ContinueName"/>. 
        /// </summary>
        public static IInputSampler GetContinue (this IInputManager mng) => mng.GetSampler(InputConfiguration.ContinueName);
        /// <summary>
        /// Attempts to <see cref="IInputManager.GetSampler(string)"/> of <see cref="InputConfiguration.SkipName"/>. 
        /// </summary>
        public static IInputSampler GetSkip (this IInputManager mng) => mng.GetSampler(InputConfiguration.SkipName);
        /// <summary>
        /// Attempts to <see cref="IInputManager.GetSampler(string)"/> of <see cref="InputConfiguration.AutoPlayName"/>. 
        /// </summary>
        public static IInputSampler GetAutoPlay (this IInputManager mng) => mng.GetSampler(InputConfiguration.AutoPlayName);
        /// <summary>
        /// Attempts to <see cref="IInputManager.GetSampler(string)"/> of <see cref="InputConfiguration.ToggleUIName"/>. 
        /// </summary>
        public static IInputSampler GetToggleUI (this IInputManager mng) => mng.GetSampler(InputConfiguration.ToggleUIName);
        /// <summary>
        /// Attempts to <see cref="IInputManager.GetSampler(string)"/> of <see cref="InputConfiguration.ShowBacklogName"/>. 
        /// </summary>
        public static IInputSampler GetShowBacklog (this IInputManager mng) => mng.GetSampler(InputConfiguration.ShowBacklogName);
        /// <summary>
        /// Attempts to <see cref="IInputManager.GetSampler(string)"/> of <see cref="InputConfiguration.RollbackName"/>. 
        /// </summary>
        public static IInputSampler GetRollback (this IInputManager mng) => mng.GetSampler(InputConfiguration.RollbackName);
        /// <summary>
        /// Attempts to <see cref="IInputManager.GetSampler(string)"/> of <see cref="InputConfiguration.CameraLookXName"/>. 
        /// </summary>
        public static IInputSampler GetCameraLookX (this IInputManager mng) => mng.GetSampler(InputConfiguration.CameraLookXName);
        /// <summary>
        /// Attempts to <see cref="IInputManager.GetSampler(string)"/> of <see cref="InputConfiguration.CameraLookYName"/>. 
        /// </summary>
        public static IInputSampler GetCameraLookY (this IInputManager mng) => mng.GetSampler(InputConfiguration.CameraLookYName);
        /// <summary>
        /// Attempts to <see cref="IInputManager.GetSampler(string)"/> of <see cref="InputConfiguration.PauseName"/>. 
        /// </summary>
        public static IInputSampler GetPause (this IInputManager mng) => mng.GetSampler(InputConfiguration.PauseName);
    }
}
