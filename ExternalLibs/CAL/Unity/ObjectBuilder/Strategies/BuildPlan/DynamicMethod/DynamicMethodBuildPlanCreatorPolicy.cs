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

namespace Microsoft.Practices.ObjectBuilder2
{
    /// <summary>
    /// An <see cref="IBuildPlanCreatorPolicy"/> implementation
    /// that constructs a build plan via dynamic IL emission.
    /// </summary>
    public class DynamicMethodBuildPlanCreatorPolicy : IBuildPlanCreatorPolicy
    {
        private IStagedStrategyChain strategies;

        /// <summary>
        /// Construct a <see cref="DynamicMethodBuildPlanCreatorPolicy"/> that
        /// uses the given strategy chain to construct the build plan.
        /// </summary>
        /// <param name="strategies"></param>
        public DynamicMethodBuildPlanCreatorPolicy(IStagedStrategyChain strategies)
        {
            this.strategies = strategies;
        }

        /// <summary>
        /// Construct a build plan.
        /// </summary>
        /// <param name="context">The current build context.</param>
        /// <param name="buildKey">The current build key.</param>
        /// <returns>The created build plan.</returns>
        public IBuildPlanPolicy CreatePlan(IBuilderContext context, object buildKey)
        {
            IDynamicBuilderMethodCreatorPolicy methodCreatorPolicy =
                context.Policies.Get<IDynamicBuilderMethodCreatorPolicy>(context.BuildKey);
            DynamicBuildPlanGenerationContext generatorContext =
                new DynamicBuildPlanGenerationContext(
                    BuildKey.GetType(buildKey), methodCreatorPolicy);

            IBuilderContext planContext = GetContext(context, buildKey, generatorContext);

            planContext.Strategies.ExecuteBuildUp(planContext);

            return new DynamicMethodBuildPlan(generatorContext.GetBuildMethod());
        }

        private IBuilderContext GetContext(IBuilderContext originalContext, object buildKey, DynamicBuildPlanGenerationContext ilContext)
        {
            return new BuilderContext(
                strategies.MakeStrategyChain(),
                originalContext.Locator,
                originalContext.Lifetime,
                originalContext.PersistentPolicies,
                originalContext.Policies,
                buildKey,
                ilContext);
        }
    }
}
