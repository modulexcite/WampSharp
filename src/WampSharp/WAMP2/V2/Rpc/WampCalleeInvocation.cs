﻿namespace WampSharp.V2.Rpc
{
    internal class WampCalleeInvocation<TMessage> where TMessage : class
    {
        private readonly IWampRpcOperationCallback mCaller;
        private readonly TMessage mOptions;
        private readonly TMessage[] mArguments;
        private readonly TMessage mArgumentsKeywords;

        public WampCalleeInvocation(IWampRpcOperationCallback caller, TMessage options, TMessage[] arguments, TMessage argumentsKeywords)
        {
            mCaller = caller;
            mOptions = options;
            mArguments = arguments;
            mArgumentsKeywords = argumentsKeywords;
        }

        public IWampRpcOperationCallback Caller
        {
            get
            {
                return mCaller;
            }
        }

        public TMessage Options
        {
            get
            {
                return mOptions;
            }
        }

        public TMessage[] Arguments
        {
            get
            {
                return mArguments;
            }
        }

        public TMessage ArgumentsKeywords
        {
            get
            {
                return mArgumentsKeywords;
            }
        }
    }
}