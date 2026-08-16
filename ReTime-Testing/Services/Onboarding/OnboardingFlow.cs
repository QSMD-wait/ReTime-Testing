namespace ReTime_Testing.Services.Onboarding
{
    /// <summary>
    /// 欢迎引导流程静态辅助类
    /// 集中判定逻辑，便于单元测试
    /// </summary>
    public static class OnboardingFlow
    {
        /// <summary>
        /// 判定本次启动是否需要显示欢迎引导
        /// 规则：forceShow 恒显示；Setting.json 已存在则无视 WelcomeShowed 直接跳过
        /// </summary>
        /// <param name="settingFileExists">全局配置文件是否已存在</param>
        /// <param name="welcomeShowed">引导是否已完成</param>
        /// <param name="forceShow">Debug 强制显示</param>
        /// <returns>是否需要显示引导</returns>
        public static bool ShouldShowWelcome(bool settingFileExists, bool welcomeShowed, bool forceShow)
        {
            return forceShow || (!settingFileExists && !welcomeShowed);
        }
    }
}