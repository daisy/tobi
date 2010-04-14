﻿//===============================================================================
// Microsoft patterns & practices
// Unity Application Block
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Practices.Unity;

namespace Microsoft.Practices.ObjectBuilder2
{
    /// <summary>
    /// Represents the context in which a build-up or tear-down operation runs.
    /// </summary>
    public class BuilderContext : IBuilderContext
    {
        readonly IStrategyChain chain;
        readonly ILifetimeContainer lifetime;
        private readonly IRecoveryStack recoveryStack = new RecoveryStack();
        readonly NamedTypeBuildKey originalBuildKey;
        private readonly IPolicyList persistentPolicies;
        readonly IPolicyList policies;
        private readonly CompositeResolverOverride resolverOverrides = new CompositeResolverOverride();

        /// <summary>
        /// Initialize a new instance of the <see cref="BuilderContext"/> class.
        /// </summary>
        protected BuilderContext() { }

        /// <summary>
        /// Initialize a new instance of the <see cref="BuilderContext"/> class with a <see cref="IStrategyChain"/>, 
        /// <see cref="ILifetimeContainer"/>, <see cref="IPolicyList"/> and the 
        /// build key used to start this build operation. 
        /// </summary>
        /// <param name="chain">The <see cref="IStrategyChain"/> to use for this context.</param>
        /// <param name="lifetime">The <see cref="ILifetimeContainer"/> to use for this context.</param>
        /// <param name="policies">The <see cref="IPolicyList"/> to use for this context.</param>
        /// <param name="originalBuildKey">Build key to start building.</param>
        /// <param name="existing">The existing object to build up.</param>
        public BuilderContext(IStrategyChain chain,
            ILifetimeContainer lifetime,
            IPolicyList policies,
            NamedTypeBuildKey originalBuildKey,
            object existing)
        {
            this.chain = chain;
            this.lifetime = lifetime;
            this.originalBuildKey = originalBuildKey;
            this.BuildKey = originalBuildKey;
            this.persistentPolicies = policies;
            this.policies = new PolicyList(persistentPolicies);
            this.Existing = existing;
        }

        /// <summary>
        /// Create a new <see cref="BuilderContext"/> using the explicitly provided
        /// values.
        /// </summary>
        /// <param name="chain">The <see cref="IStrategyChain"/> to use for this context.</param>
        /// <param name="lifetime">The <see cref="ILifetimeContainer"/> to use for this context.</param>
        /// <param name="persistentPolicies">The set of persistent policies to use for this context.</param>
        /// <param name="transientPolicies">The set of transient policies to use for this context. It is
        /// the caller's responsibility to ensure that the transient and persistent policies are properly
        /// combined.</param>
        /// <param name="buildKey">Build key for this context.</param>
        /// <param name="existing">Existing object to build up.</param>
        public BuilderContext(IStrategyChain chain, ILifetimeContainer lifetime, IPolicyList persistentPolicies, IPolicyList transientPolicies, NamedTypeBuildKey buildKey, object existing)
        {
            this.chain = chain;
            this.lifetime = lifetime;
            this.persistentPolicies = persistentPolicies;
            this.policies = transientPolicies;
            this.originalBuildKey = buildKey;
            this.BuildKey = buildKey;
            this.Existing = existing;
        }

        /// <summary>
        /// Gets the head of the strategy chain.
        /// </summary>
        /// <returns>
        /// The strategy that's first in the chain; returns null if there are no
        /// strategies in the chain.
        /// </returns>
        public IStrategyChain Strategies
        {
            get { return chain; }
        }

        /// <summary>
        /// Get the current build key for the current build operation.
        /// </summary>
        public NamedTypeBuildKey BuildKey { get; set; }

        /// <summary>
        /// The current object being built up or torn down.
        /// </summary>
        /// <value>
        /// The current object being manipulated by the build operation. May
        /// be null if the object hasn't been created yet.</value>
        public object Existing { get; set; }

        /// <summary>
        /// Gets the <see cref="ILifetimeContainer"/> associated with the build.
        /// </summary>
        /// <value>
        /// The <see cref="ILifetimeContainer"/> associated with the build.
        /// </value>
        public ILifetimeContainer Lifetime
        {
            get { return lifetime; }
        }

        /// <summary>
        /// Gets the original build key for the build operation.
        /// </summary>
        /// <value>
        /// The original build key for the build operation.
        /// </value>
        public NamedTypeBuildKey OriginalBuildKey
        {
            get { return originalBuildKey; }
        }

        /// <summary>
        /// The set of policies that were passed into this context.
        /// </summary>
        /// <remarks>This returns the policies passed into the context.
        /// Policies added here will remain after buildup completes.</remarks>
        /// <value>The persistent policies for the current context.</value>
        public IPolicyList PersistentPolicies
        {
            get { return persistentPolicies; }
        }

        /// <summary>
        /// Gets the policies for the current context. 
        /// </summary>
        /// <remarks>
        /// Any modifications will be transient (meaning, they will be forgotten when 
        /// the outer BuildUp for this context is finished executing).
        /// </remarks>
        /// <value>
        /// The policies for the current context.
        /// </value>
        public IPolicyList Policies
        {
            get { return policies; }
        }


        /// <summary>
        /// Gets the collection of <see cref="IRequiresRecovery"/> objects
        /// that need to execute in event of an exception.
        /// </summary>
        public IRecoveryStack RecoveryStack
        {
            get { return recoveryStack; }
        }

        /// <summary>
        /// Flag indicating if the build operation should continue.
        /// </summary>
        /// <value>true means that building should not call any more
        /// strategies, false means continue to the next strategy.</value>
        public bool BuildComplete { get; set; }

        /// <summary>
        /// An object representing what is currently being done in the
        /// build chain. Used to report back errors if there's a failure.
        /// </summary>
        public object CurrentOperation { get; set; }

        /// <summary>
        /// The build context used to resolve a dependency during the build operation represented by this context.
        /// </summary>
        public IBuilderContext ChildContext { get; private set; }

        /// <summary>
        /// Add a new set of resolver override objects to the current build operation.
        /// </summary>
        /// <param name="newOverrides"><see cref="ResolverOverride"/> objects to add.</param>
        public void AddResolverOverrides(IEnumerable<ResolverOverride> newOverrides)
        {
            resolverOverrides.AddRange(newOverrides);
        }

        /// <summary>
        /// Get a <see cref="IDependencyResolverPolicy"/> object for the given <paramref name="dependencyType"/>
        /// or null if that dependency hasn't been overridden.
        /// </summary>
        /// <param name="dependencyType">Type of the dependency.</param>
        /// <returns>Resolver to use, or null if no override matches for the current operation.</returns>
        public IDependencyResolverPolicy GetOverriddenResolver(Type dependencyType)
        {
            return resolverOverrides.GetResolver(this, dependencyType);
        }

        /// <summary>
        /// A convenience method to do a new buildup operation on an existing context.
        /// </summary>
        /// <param name="newBuildKey">Key to use to build up.</param>
        /// <returns>Created object.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "BuildUp",
            Justification = "Kept for backward compatibility with ObjectBuilder")]
        public object NewBuildUp(NamedTypeBuildKey newBuildKey)
        {
            this.ChildContext =
                new BuilderContext(chain, lifetime, persistentPolicies, policies, newBuildKey, null);

            ChildContext.AddResolverOverrides(Sequence.Collect(resolverOverrides));

            object result = this.ChildContext.Strategies.ExecuteBuildUp(this.ChildContext);

            this.ChildContext = null;

            return result;
        }

        /// <summary>
        /// A convenience method to do a new buildup operation on an existing context. This
        /// overload allows you to specify extra policies which will be in effect for the duration
        /// of the build.
        /// </summary>
        /// <param name="newBuildKey">Key defining what to build up.</param>
        /// <param name="childCustomizationBlock">A delegate that takes a <see cref="IBuilderContext"/>. This
        /// is invoked with the new child context before the build up process starts. This gives callers
        /// the opportunity to customize the context for the build process.</param>
        /// <returns>Created object.</returns>
        public object NewBuildUp(NamedTypeBuildKey newBuildKey, Action<IBuilderContext> childCustomizationBlock)
        {
            ChildContext =
                new BuilderContext(chain, lifetime, persistentPolicies, policies, newBuildKey, null);

            ChildContext.AddResolverOverrides(Sequence.Collect(resolverOverrides));

            childCustomizationBlock(ChildContext);

            object result = ChildContext.Strategies.ExecuteBuildUp(ChildContext);

            ChildContext = null;

            return result;
        }
    }
}
