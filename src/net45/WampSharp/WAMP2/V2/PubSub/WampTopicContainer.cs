using System;
using System.Collections.Generic;
using System.Linq;
using WampSharp.Core.Serialization;
using WampSharp.V2.Core;
using WampSharp.V2.Core.Contracts;

namespace WampSharp.V2.PubSub
{
    /// <summary>
    /// A default implementation of <see cref="IWampTopicContainer"/>.
    /// </summary>
    public class WampTopicContainer : IWampTopicContainer
    {
        #region Fields

        private readonly WampIdGenerator mGenerator = new WampIdGenerator();
        private readonly MatchTopicContainer[] mInnerContainers;
        private readonly ExactTopicContainer mExactTopicContainer;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of <see cref="WampTopicContainer"/>.
        /// </summary>
        public WampTopicContainer()
        {
            mExactTopicContainer = new ExactTopicContainer();

            mInnerContainers = new MatchTopicContainer[]
            {
                mExactTopicContainer,
                new PrefixTopicContainer(),
                new WildCardTopicContainer()
            };
        }

        #endregion

        public IEnumerable<string> TopicUris
        {
            get { return mExactTopicContainer.TopicUris; }
        }

        public IEnumerable<IWampTopic> Topics
        {
            get { return mExactTopicContainer.Topics; }
        }

        public IDisposable Subscribe(IWampRawTopicRouterSubscriber subscriber, string topicUri, SubscribeOptions options)
        {
            MatchTopicContainer topicContainer = GetInnerContainer(options);

            return topicContainer.Subscribe(subscriber, topicUri, options);
        }

        public IWampCustomizedSubscriptionId GetSubscriptionId(string topicUri, SubscribeOptions options)
        {
            MatchTopicContainer topicContainer = GetInnerContainer(options);

            return topicContainer.GetSubscriptionId(topicUri, options);
        }

        private MatchTopicContainer GetInnerContainer(SubscribeOptions options)
        {
            MatchTopicContainer topicContainer = mInnerContainers.FirstOrDefault(x => x.Handles(options));

            if (topicContainer == null)
            {
                throw new WampException(WampErrors.InvalidTopic,
                    "unknown match type: " + options.Match);
            }

            return topicContainer;
        }

        public long Publish<TMessage>(IWampFormatter<TMessage> formatter, PublishOptions options, string topicUri)
        {
            Func<MatchTopicContainer, long, bool> publishAction =
                (container, publicationId) =>
                    container.Publish(formatter, publicationId, options, topicUri);

            return InnerPublish(publishAction, topicUri);
        }

        public long Publish<TMessage>(IWampFormatter<TMessage> formatter, PublishOptions options, string topicUri, TMessage[] arguments)
        {
            Func<MatchTopicContainer, long, bool> publishAction =
                (container, publicationId) =>
                    container.Publish(formatter, publicationId, options, topicUri, arguments);

            return InnerPublish(publishAction, topicUri);
        }

        public long Publish<TMessage>(IWampFormatter<TMessage> formatter, PublishOptions options, string topicUri, TMessage[] arguments,
            IDictionary<string, TMessage> argumentKeywords)
        {
            Func<MatchTopicContainer, long, bool> publishAction =
                (container, publicationId) =>
                    container.Publish(formatter, publicationId, options, topicUri, arguments, argumentKeywords);

            return InnerPublish(publishAction, topicUri);
        }

        private long InnerPublish(Func<MatchTopicContainer, long, bool> publishAction, string topicUri)
        {
            bool published = false;

            long publicationId = mGenerator.Generate();

            foreach (MatchTopicContainer container in mInnerContainers)
            {
                published =
                    published | publishAction(container, publicationId);
            }

            if (published)
            {
                return publicationId;
            }
            else
            {
                throw new WampException(WampErrors.InvalidTopic,
                                 "topicUri: " + topicUri);
            }
        }

        public IWampTopic CreateTopicByUri(string topicUri, bool persistent)
        {
            return mExactTopicContainer.CreateTopicByUri(topicUri, persistent);
        }

        public IWampTopic GetOrCreateTopicByUri(string topicUri)
        {
            return mExactTopicContainer.GetOrCreateTopicByUri(topicUri);
        }

        public IWampTopic GetTopicByUri(string topicUri)
        {
            return mExactTopicContainer.GetTopicByUri(topicUri);
        }

        public bool TryRemoveTopicByUri(string topicUri, out IWampTopic topic)
        {
            return mExactTopicContainer.TryRemoveTopicByUri(topicUri, out topic);
        }

        public event EventHandler<WampTopicCreatedEventArgs> TopicCreated
        {
            add
            {
                foreach (MatchTopicContainer container in mInnerContainers)
                {
                    container.TopicCreated += value;
                }
            }
            remove
            {
                foreach (MatchTopicContainer container in mInnerContainers)
                {
                    container.TopicCreated -= value;
                }
            }
        }

        public event EventHandler<WampTopicRemovedEventArgs> TopicRemoved
        {
            add
            {
                foreach (MatchTopicContainer container in mInnerContainers)
                {
                    container.TopicRemoved += value;
                }
            }
            remove
            {
                foreach (MatchTopicContainer container in mInnerContainers)
                {
                    container.TopicRemoved -= value;
                }
            }
        }
    }
}