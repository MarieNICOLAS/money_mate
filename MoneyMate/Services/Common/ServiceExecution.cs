using MoneyMate.Services.Results;
using System.Diagnostics;

namespace MoneyMate.Services.Common
{
    /// <summary>
    /// Fournit des helpers pour exécuter les services avec gestion d'erreur homogène,
    /// sans répéter partout les mêmes blocs try/catch.
    /// </summary>
    public static class ServiceExecution
    {
        public static Task<ServiceResult> ExecuteAsync(
            Func<ServiceResult> action,
            string operationName,
            string fallbackErrorCode,
            string fallbackMessage)
        {
            return Task.Run(() =>
            {
                try
                {
                    return action();
                }
                catch (Exception ex)
                {
                    LogException(operationName, ex);
                    return ServiceResult.Failure(
                        fallbackErrorCode,
                        fallbackMessage);
                }
            });
        }

        public static Task<ServiceResult<T>> ExecuteAsync<T>(
            Func<ServiceResult<T>> action,
            string operationName,
            string fallbackErrorCode,
            string fallbackMessage)
        {
            return Task.Run(() =>
            {
                try
                {
                    return action();
                }
                catch (Exception ex)
                {
                    LogException(operationName, ex);
                    return ServiceResult<T>.Failure(
                        fallbackErrorCode,
                        fallbackMessage);
                }
            });
        }

        public static Task<ServiceResult<T>> ExecuteAsync<T>(
            Func<T> action,
            string operationName,
            string fallbackErrorCode,
            string fallbackMessage)
        {
            return Task.Run(() =>
            {
                try
                {
                    T data = action();
                    return ServiceResult<T>.Success(data);
                }
                catch (Exception ex)
                {
                    LogException(operationName, ex);
                    return ServiceResult<T>.Failure(
                        fallbackErrorCode,
                        fallbackMessage);
                }
            });
        }

        public static ServiceResult Execute(
            Func<ServiceResult> action,
            string operationName,
            string fallbackErrorCode,
            string fallbackMessage)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                LogException(operationName, ex);
                return ServiceResult.Failure(
                    fallbackErrorCode,
                    fallbackMessage);
            }
        }

        public static ServiceResult<T> Execute<T>(
            Func<ServiceResult<T>> action,
            string operationName,
            string fallbackErrorCode,
            string fallbackMessage)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                LogException(operationName, ex);
                return ServiceResult<T>.Failure(
                    fallbackErrorCode,
                    fallbackMessage);
            }
        }

        public static ServiceResult<T> Execute<T>(
            Func<T> action,
            string operationName,
            string fallbackErrorCode,
            string fallbackMessage)
        {
            try
            {
                T data = action();
                return ServiceResult<T>.Success(data);
            }
            catch (Exception ex)
            {
                LogException(operationName, ex);
                return ServiceResult<T>.Failure(
                    fallbackErrorCode,
                    fallbackMessage);
            }
        }

        private static void LogException(string operationName, Exception ex)
        {
#if DEBUG
            Debug.WriteLine($"[SERVICE ERROR] {operationName}: {ex}");
#endif
        }
    }
}
