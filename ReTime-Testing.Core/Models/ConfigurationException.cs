namespace ReTime_Testing.Models
{
    /// <summary>
    /// 配置异常
    /// </summary>
    public class ConfigurationException : Exception
    {
        /// <summary>
        /// 初始化配置异常
        /// </summary>
        public ConfigurationException()
        {
        }

        /// <summary>
        /// 初始化配置异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public ConfigurationException(string message) : base(message)
        {
        }

        /// <summary>
        /// 初始化配置异常
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public ConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}