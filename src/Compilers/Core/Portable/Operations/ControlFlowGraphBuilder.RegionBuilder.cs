﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Operations
{
    internal sealed partial class ControlFlowGraphBuilder
    {
        private class RegionBuilder
        {
            public readonly ControlFlowGraph.RegionKind Kind;
            public RegionBuilder Enclosing { get; private set; } = null;
            public readonly ITypeSymbol ExceptionType;
            public BasicBlock FirstBlock = null;
            public BasicBlock LastBlock = null;
            public ArrayBuilder<RegionBuilder> Regions = null;
            public ImmutableArray<ILocalSymbol> Locals;
#if DEBUG
            private bool _aboutToFree = false;
#endif 

            public RegionBuilder(ControlFlowGraph.RegionKind kind, ITypeSymbol exceptionType = null, ImmutableArray<ILocalSymbol> locals = default)
            {
                Kind = kind;
                ExceptionType = exceptionType;
                Locals = locals.NullToEmpty();
            }

            public bool IsEmpty => FirstBlock == null;
            public bool HasRegions => Regions?.Count > 0;

#if DEBUG
            public void AboutToFree() => _aboutToFree = true;
#endif 

            public void Add(RegionBuilder region)
            {
                if (Regions == null)
                {
                    Regions = ArrayBuilder<RegionBuilder>.GetInstance();
                }

#if DEBUG
                Debug.Assert(region.Enclosing == null || (region.Enclosing._aboutToFree && region.Enclosing.Enclosing == this));
#endif 
                region.Enclosing = this;
                Regions.Add(region);

#if DEBUG
                ControlFlowGraph.RegionKind lastKind = Regions.Last().Kind;
                switch (Kind)
                {
                    case ControlFlowGraph.RegionKind.FilterAndHandler:
                        Debug.Assert(Regions.Count <= 2);
                        Debug.Assert(lastKind == (Regions.Count < 2 ? ControlFlowGraph.RegionKind.Filter : ControlFlowGraph.RegionKind.Handler));
                        break;

                    case ControlFlowGraph.RegionKind.TryAndCatch:
                        if (Regions.Count == 1)
                        {
                            Debug.Assert(lastKind == ControlFlowGraph.RegionKind.Try);
                        }
                        else
                        {
                            Debug.Assert(lastKind == ControlFlowGraph.RegionKind.Handler || lastKind == ControlFlowGraph.RegionKind.FilterAndHandler);
                        }
                        break;

                    case ControlFlowGraph.RegionKind.TryAndFinally:
                        Debug.Assert(Regions.Count <= 2);
                        if (Regions.Count == 1)
                        {
                            Debug.Assert(lastKind == ControlFlowGraph.RegionKind.Try);
                        }
                        else
                        {
                            Debug.Assert(lastKind == ControlFlowGraph.RegionKind.Handler);
                        }
                        break;

                    default:
                        Debug.Assert(lastKind != ControlFlowGraph.RegionKind.Filter);
                        Debug.Assert(lastKind != ControlFlowGraph.RegionKind.Handler);
                        Debug.Assert(lastKind != ControlFlowGraph.RegionKind.Try);
                        break;
                }
#endif
            }

            public void ExtendToInclude(BasicBlock block)
            {
                Debug.Assert((Kind != ControlFlowGraph.RegionKind.FilterAndHandler &&
                              Kind != ControlFlowGraph.RegionKind.TryAndCatch &&
                              Kind != ControlFlowGraph.RegionKind.TryAndFinally) ||
                              Regions.Last().LastBlock == block);

                if (FirstBlock == null)
                {
                    Debug.Assert(LastBlock == null);

                    if (!HasRegions)
                    {
                        FirstBlock = block;
                        LastBlock = block;
                        return;
                    }

                    FirstBlock = Regions.First().FirstBlock;
                    Debug.Assert(Regions.Count == 1 && Regions.First().LastBlock == block);
                }
                else
                {
                    Debug.Assert(LastBlock.Ordinal < block.Ordinal);
                    Debug.Assert(!HasRegions || Regions.Last().LastBlock.Ordinal <= block.Ordinal);
                }

                LastBlock = block;
            }

            public void Free()
            {
#if DEBUG
                Debug.Assert(_aboutToFree);
#endif 
                Enclosing = null;
                FirstBlock = null;
                LastBlock = null;
                Regions?.Free();
                Regions = null;
            }

            public ControlFlowGraph.Region ToImmutableRegionAndFree(ArrayBuilder<BasicBlock> blocks)
            {
#if DEBUG
                Debug.Assert(!_aboutToFree);
#endif 
                Debug.Assert(!IsEmpty);

                ImmutableArray<ControlFlowGraph.Region> subRegions;

                if (HasRegions)
                {
                    var builder = ArrayBuilder<ControlFlowGraph.Region>.GetInstance(Regions.Count);

                    foreach (RegionBuilder region in Regions)
                    {
                        builder.Add(region.ToImmutableRegionAndFree(blocks));
                    }

                    subRegions = builder.ToImmutableAndFree();
                }
                else
                {
                    subRegions = ImmutableArray<ControlFlowGraph.Region>.Empty;
                }

                var result = new ControlFlowGraph.Region(Kind, FirstBlock.Ordinal, LastBlock.Ordinal, subRegions, Locals, ExceptionType);

                int firstBlockWithoutRegion = FirstBlock.Ordinal;

                foreach (ControlFlowGraph.Region region in subRegions)
                {
                    for (int i = firstBlockWithoutRegion; i < region.FirstBlockOrdinal; i++)
                    {
                        Debug.Assert(blocks[i].Region == null);
                        blocks[i].Region = result;
                    }

                    firstBlockWithoutRegion = region.LastBlockOrdinal + 1;
                }

                for (int i = firstBlockWithoutRegion; i <= LastBlock.Ordinal; i++)
                {
                    Debug.Assert(blocks[i].Region == null);
                    blocks[i].Region = result;
                }

#if DEBUG
                AboutToFree();
#endif 
                Free();
                return result;
            }
        }
    }
}