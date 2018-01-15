using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace AzureStorage
{
    internal abstract class ExplicitAppInsightsCallDecoratorBase
    {
        protected readonly TelemetryClient _telemetry = new TelemetryClient();

        protected abstract string TrackType { get; }

        protected async Task<TResult> WrapAsync<TResult>(Func<Task<TResult>> func, string name, [CallerMemberName] string caller = "")
        {
            var operation = InitOperation(name, caller);
            try
            {
                return await func();
            }
            catch (Exception e)
            {
                operation.Telemetry.Success = false;
                _telemetry.TrackException(e);
                throw;
            }
            finally
            {
                _telemetry.StopOperation(operation);
            }
        }

        protected async Task WrapAsync(Func<Task> func, string name, [CallerMemberName] string caller = "")
        {
            var operation = InitOperation(name, caller);
            try
            {
                await func();
            }
            catch (Exception e)
            {
                operation.Telemetry.Success = false;
                _telemetry.TrackException(e);
                throw;
            }
            finally
            {
                _telemetry.StopOperation(operation);
            }
        }

        protected TResult Wrap<TResult>(Func<TResult> func, string name, [CallerMemberName] string caller = "")
        {
            var operation = InitOperation(name, caller);
            try
            {
                return func();
            }
            catch (Exception e)
            {
                operation.Telemetry.Success = false;
                _telemetry.TrackException(e);
                throw;
            }
            finally
            {
                _telemetry.StopOperation(operation);
            }
        }

        protected void Wrap(Action func, string name, [CallerMemberName] string caller = "")
        {
            var operation = InitOperation(name, caller);
            try
            {
                func();
            }
            catch (Exception e)
            {
                operation.Telemetry.Success = false;
                _telemetry.TrackException(e);
                throw;
            }
            finally
            {
                _telemetry.StopOperation(operation);
            }
        }

        private IOperationHolder<DependencyTelemetry> InitOperation(string name, string caller)
        {
            var operation = _telemetry.StartOperation<DependencyTelemetry>(caller);
            operation.Telemetry.Type = TrackType;
            operation.Telemetry.Target = name;
            operation.Telemetry.Name = caller;
            operation.Telemetry.Data = caller;

            return operation;
        }
    }
}
