﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WampSharp.Core.Serialization;
using WampSharp.Logging;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.Error;

namespace WampSharp.V2.Rpc
{
    public abstract class AsyncLocalRpcOperation: LocalRpcOperation
    {
        protected AsyncLocalRpcOperation(string procedure) : base(procedure)
        {
        }

        protected abstract Task<object> InvokeAsync<TMessage>
            (IWampRawRpcOperationRouterCallback caller, IWampFormatter<TMessage> formatter, InvocationDetails details, TMessage[] arguments, IDictionary<string, TMessage> argumentsKeywords);

#if NET45

        protected override async void InnerInvoke<TMessage>(IWampRawRpcOperationRouterCallback caller,
                                                            IWampFormatter<TMessage> formatter,
                                                            InvocationDetails details,
                                                            TMessage[] arguments,
                                                            IDictionary<string, TMessage> argumentsKeywords)
        {
            try
            {
                Task<object> task =
                    InvokeAsync(caller,
                                formatter,
                                details,
                                arguments,
                                argumentsKeywords);

                object result = await task;

                CallResult(caller, result, null);
            }
            catch (Exception ex)
            {
                mLogger.ErrorFormat(ex, "An error occured while calling {0}", this.Procedure);

                WampException wampException = ex as WampException;

                if (wampException == null)
                {
                    wampException = ConvertExceptionToRuntimeException(ex);
                }

                IWampErrorCallback callback = new WampRpcErrorCallback(caller);

                callback.Error(wampException);
            }
        }

#elif NET40

        protected override void InnerInvoke<TMessage>(IWampRawRpcOperationRouterCallback caller, IWampFormatter<TMessage> formatter, InvocationDetails options, TMessage[] arguments, IDictionary<string, TMessage> argumentsKeywords)
        {
            try
            {
                Task<object> task =
                    InvokeAsync(caller,
                               formatter,
                               options,
                               arguments,
                               argumentsKeywords);

                task.ContinueWith(x => TaskCallback(x, caller));
            }
            catch (WampException ex)
            {
                mLogger.ErrorFormat(ex, "An error occured while calling {0}", this.Procedure);
                IWampErrorCallback callback = new WampRpcErrorCallback(caller);
                callback.Error(ex);
            }
        }

        private void TaskCallback(Task<object> task, IWampRawRpcOperationRouterCallback caller)
        {
            if (task.Exception == null)
            {
                object result = task.Result;
                CallResult(caller, result, null);
            }
            else
            {
                Exception innerException = task.Exception.InnerException;

                mLogger.ErrorFormat(innerException, "An error occured while calling {0}", this.Procedure);

                WampException wampException = innerException as WampException;
                
                if (wampException == null)
                {
                    wampException = ConvertExceptionToRuntimeException(innerException);
                }

                IWampErrorCallback callback = new WampRpcErrorCallback(caller);
                
                callback.Error(wampException);
            }
        }

#endif
    }
}