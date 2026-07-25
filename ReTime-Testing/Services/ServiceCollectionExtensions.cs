using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReTime_Testing.ViewModels;
using ReTime_Testing.ViewModels.TimeScheduleEditor;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// DI 服务注册扩展方法
    /// 集中管理所有服务、ViewModel 的注册和生命周期
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册所有 ReTime 服务
        /// </summary>
        public static IServiceCollection AddReTimeServices(this IServiceCollection services)
        {
            // ===== 基础设施 =====
            services.AddSingleton<IConfigurationManager, ConfigurationManager>();
            services.AddSingleton<ISettingsService, SettingsService>();

            // ===== 时间服务 =====
            services.AddSingleton<ITimeService, AbsoluteTimeService>();
            services.AddSingleton<ICloudCalibrationService, CloudCalibrationService>();
            services.AddSingleton<ITimeCalibrationService, TimeCalibrationService>();

            // ===== 调度与状态 =====
            services.AddSingleton<IProgressStateManager, ProgressStateManager>();
            services.AddSingleton<IGlobalTimeTopDesktopService, GlobalTimeTopDesktopService>();
            services.AddSingleton<IScheduleManager, ScheduleManager>();
            services.AddSingleton<ITimeScheduleManager, TimeScheduleManager>();
            services.AddSingleton<IScheduleGroupManager, ScheduleGroupManager>();

            // ===== UI 服务 =====
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IAutoStartService, AutoStartService>();
            services.AddSingleton<ITrayIconService, TrayIconService>();
            services.AddSingleton<IDesktopWindowManager, DesktopWindowManager>();
            services.AddSingleton<ITopmostService, TopmostService>();
            services.AddSingleton<IMutexManager, MutexManager>();

            // ===== ViewModel（Transient：每次请求新实例） =====
            services.AddTransient<TimeTopDesktopViewModel>();
            services.AddTransient<TimeTopSettingViewModel>();
            services.AddTransient<DebugTestViewModel>();
            services.AddTransient<TimePageViewModel>();
            services.AddTransient<TimeScheduleEditorViewModel>();

            return services;
        }
    }
}