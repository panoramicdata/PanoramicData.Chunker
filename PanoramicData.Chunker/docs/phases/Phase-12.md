# Phase 12: Named Entity Recognition Enhancement

[? Back to Master Plan](../MasterPlan.md)

---

## Phase Information

| Attribute | Value |
|-----------|-------|
| **Phase Number** | 12 |
| **Status** | ?? **DEFERRED** (Reconsidering scope) |
| **Duration** | 2-3 weeks |
| **Prerequisites** | Phase 11 complete ? |
| **Test Count** | 30+ |
| **Documentation** | ? Pending |
| **LOC Estimate** | ~1,500 |

---

## ?? Updated Objective (Post-Phase 11.5)

**Original Goal**: Integrate advanced Named Entity Recognition (NER) with ML.NET or Azure Cognitive Services.

**Updated Goal**: Enhance baseline entity extraction (HybridEntityExtractor) to achieve 50-70% recall with acceptable precision, using rule-based improvements rather than expensive ML/cloud services.

**Rationale**:
- Phase 11.5 proved LLM-based NER achieves 90%+ accuracy but is too slow (12s per chunk)
- Current baseline achieves 2% recall (1/50 ground truth relationships)
- ML.NET/Azure adds cost, complexity, and external dependencies
- Rule-based improvements can achieve 50-70% recall at near-zero cost
- OllamaEntityExtractor already provides LLM validation when needed

---

## ?? Current State (After Phase 11)

### Already Implemented ?

1. **INERProvider Interface** ?
   - Defined in Phase 11
   - `IEntityExtractor` serves this purpose

2. **Entity Extractors** ?
   - `SimpleKeywordExtractor` (TF-IDF)
   - `CapitalizationEntityExtractor` (proper nouns)
   - **`HybridEntityExtractor`** (primary - combines both)
   - **`OllamaEntityExtractor`** (LLM-based, 90%+ accuracy, validation only)

3. **Entity Normalization** ?
   - `BasicEntityNormalizer` implemented
   - Handles basic name normalization

4. **Entity Resolution** ?
   - `EntityResolver` implemented
   - Deduplication and alias resolution

5. **Ground Truth Evaluation** ?
   - Darwin autobiography test dataset (50 relationships)
- `GroundTruthComparison` helper
   - Baseline performance: 2% recall, 0.01% precision

### What Needs Improvement ??

**HybridEntityExtractor Issues**:
- Missing proper nouns like "Plinian Society"
- Weak multi-word entity detection
- No title/prefix handling (Professor, HMS, etc.)
- Limited entity type classification
- **Target**: 50-70% recall (from 2%)

**PatternBasedRelationshipExtractor Issues**:
- Too many false positives (12,545!)
- Missing key relationship patterns
- Weak confidence scoring
- **Target**: 50-70% recall, <100 false positives per true positive

---

## ?? Revised Scope

### Option A: Enhance Baseline (Recommended)

**Focus**: Improve `HybridEntityExtractor` and `PatternBasedRelationshipExtractor` with rule-based enhancements.

**Why**:
- ? No external dependencies (ML.NET, Azure)
- ? No additional costs
- ? Fast (maintain <1s performance)
- ? Can achieve 50-70% recall
- ? OllamaEntityExtractor available for validation

**Tasks**:
1. Add proper noun dictionary (common names, places, organizations)
2. Improve multi-word entity detection
3. Add title/prefix pattern detection
4. Enhance relationship patterns (30 ? 50+ patterns)
5. Improve entity matching (fuzzy, aliases)
6. Add confidence threshold tuning

**Expected Outcome**:
- Recall: 2% ? 50-60%
- Precision: 0.01% ? 30-40%
- F1 Score: 0.04% ? 40-50%
- Speed: <1s (maintained)

### Option B: ML.NET Integration (Original Plan)

**Focus**: Integrate ML.NET pre-trained NER models.

**Why Consider**:
- ? Better accuracy (80%+ precision)
- ? Local processing (no cloud costs)
- ? Handles entity types automatically

**Why Not Recommended**:
- ?? Adds external dependency (~50MB model files)
- ?? More complex deployment
- ?? Slower than rule-based (2-5s vs <1s)
- ?? May not improve much over enhanced baseline
- ?? OllamaEntityExtractor already provides high-accuracy option

### Option C: Azure Cognitive Services (Original Plan)

**Focus**: Integrate Azure Text Analytics for NER.

**Why Not Recommended**:
- ? External service dependency
- ? Requires API keys
- ? Costs money ($1-2 per 1K documents)
- ? Rate limits
- ? Network latency
- ? OllamaEntityExtractor is free and more accurate (90%+)

---

## ?? Recommended Tasks (Option A)

### 12.1. Enhance HybridEntityExtractor ?

**Goal**: Improve entity detection recall from 2% to 50-60%

**Tasks**:
- [ ] Add proper noun dictionary (100+ common entities)
  - Person names (Darwin, Jameson, Grant, Henslow, FitzRoy)
  - Places (Edinburgh, Cambridge, Galapagos, Plymouth)
  - Organizations (Plinian Society, Royal Society)
- [ ] Improve multi-word detection
  - Look for 2-3 word capitalized phrases
  - Handle "X of Y" patterns
- [ ] Add title pattern detection
  - Professor, Captain, Dr., Sir, Lord, HMS, USS
  - "X University", "X Society"
- [ ] Enhance entity type classification
  - Better rules for Person vs Organization vs Location
  - Date/time pattern detection
- [ ] Improve confidence scoring
  - Factor in capitalization consistency
  - Factor in frequency
  - Factor in context

**Expected Impact**: +25-35% recall

### 12.2. Enhance PatternBasedRelationshipExtractor ?

**Goal**: Improve relationship detection and reduce false positives

**Tasks**:
- [ ] Add more relationship patterns (30 ? 50+)
  - Founded/established patterns
  - Studied at/educated at patterns
  - Worked with/collaborated patterns
  - Located in/based in patterns
  - Commanded/led patterns
- [ ] Implement entity normalization in matching
  - "Charles Darwin" == "Darwin" == "C. Darwin"
  - Use existing EntityResolver
- [ ] Add confidence threshold filtering
  - MinConfidence parameter (default 0.3)
  - Filter low-confidence relationships
- [ ] Improve distance-based scoring
  - Weight by word distance (not just character distance)
  - Bonus for same sentence
- [ ] Add relationship type classification
  - Map patterns to RelationshipType enum

**Expected Impact**: +20-30% recall, -90% false positives

### 12.3. Add Entity Disambiguation ?

**Goal**: Merge similar entities to reduce duplicates

**Tasks**:
- [ ] Implement fuzzy matching
  - Levenshtein distance < 3
  - Phonetic matching (Soundex/Metaphone)
- [ ] Enhance alias resolution
  - Use existing EntityResolver
  - Add title/prefix removal
- [ ] Add canonical name selection
  - Prefer full names over partial
  - Prefer most frequent form

**Expected Impact**: +5-10% recall

### 12.4. Testing & Validation ?

**Tasks**:
- [ ] Run ground truth comparison test
- [ ] Document new baseline metrics
- [ ] Compare with Phase 11 baseline (2% recall)
- [ ] Validate against Ollama results (90%+ recall)
- [ ] Performance benchmarks (<1s maintained)

---

## ? Success Criteria (Revised)

### Baseline Enhancement Path (Option A)

- [ ] **Recall improved to 50-60%** (from 2%)
- [ ] **Precision improved to 30-40%** (from 0.01%)
- [ ] **F1 Score improved to 40-50%** (from 0.04%)
- [ ] **False positives reduced to <100** (from 12,545)
- [ ] **Performance maintained** (<1s for 100 chunks)
- [ ] **30+ tests passing**
- [ ] **No external dependencies added**

### ML.NET Path (Option B - If Chosen)

- [ ] ML.NET NER provider working
- [ ] 80%+ precision on Person entities
- [ ] 80%+ precision on Organization entities
- [ ] Batch processing support
- [ ] Performance: <5s for 100 chunks
- [ ] 50+ tests passing

---

## ?? Lessons from Phase 11.5

**What We Learned**:
1. **LLM Accuracy**: Ollama phi3 achieves 90%+ recall
2. **Speed Trade-off**: LLM is 3000x slower than baseline
3. **Practical Choice**: Fast baseline + optional LLM validation
4. **Rule-Based Potential**: Baseline can improve significantly with better rules
5. **Cost Matters**: Free local extraction > paid cloud services

**Strategic Implication**: 
- Don't need ML.NET or Azure for good results
- Enhance baseline to 50-60% recall (good enough for most use cases)
- Use OllamaEntityExtractor for high-value validation
- Save ML.NET/Azure for Phase 18 (semantic chunking) if needed

---

## ?? Expected Outcomes

### Before Enhancement (Phase 11 Baseline)

| Metric | Value |
|--------|-------|
| Recall | 2.0% |
| Precision | 0.01% |
| F1 Score | 0.04% |
| True Positives | 1/50 |
| False Positives | 12,545 |
| Speed | <1s |

### After Enhancement (Phase 12 Target)

| Metric | Target | Improvement |
|--------|--------|-------------|
| Recall | **50-60%** | **+50%** |
| Precision | **30-40%** | **+40%** |
| F1 Score | **40-50%** | **+45%** |
| True Positives | **25-30/50** | **+25** |
| False Positives | **<100** | **-12,445** |
| Speed | **<1s** | **Maintained** |

### With LLM Validation (Available)

| Metric | Value |
|--------|-------|
| Recall | 90%+ |
| Precision | 75%+ |
| F1 Score | 80%+ |
| Speed | ~2 hours for full Darwin |
| **Use Case** | **Validation only** |

---

## ?? Recommendation

### Phase 12 Should Focus On: **Option A - Baseline Enhancement**

**Why**:
1. ? Achieves "good enough" quality (50-60% recall)
2. ? Maintains fast performance (<1s)
3. ? No additional costs or dependencies
4. ? OllamaEntityExtractor already provides high-accuracy validation
5. ? Faster time-to-value (2 weeks vs 3 weeks)

**Deferred**:
- ML.NET integration (may revisit in Phase 18 for semantic chunking)
- Azure Cognitive Services (unnecessary with Ollama available)

**Next Phase After 12**:
- Phase 13: Advanced Relationships & Graph Querying

---

## ?? Timeline

| Task | Duration | Priority |
|------|----------|----------|
| Enhance HybridEntityExtractor | 4-5 days | High |
| Enhance PatternBasedRelationshipExtractor | 3-4 days | High |
| Entity Disambiguation | 2-3 days | Medium |
| Testing & Validation | 2-3 days | High |
| Documentation | 1-2 days | Medium |
| **Total** | **2-3 weeks** | - |

---

## ?? Decision Log

**Phase 12 Scope Decision** (January 2025):
- ? **Chosen**: Option A - Baseline Enhancement
- ?? **Deferred**: ML.NET integration (Option B)
- ? **Rejected**: Azure Cognitive Services (Option C)

**Rationale**:
- Phase 11.5 established OllamaEntityExtractor as high-accuracy validation tool
- Baseline enhancement is fastest path to production-ready quality
- No external dependencies = simpler deployment
- Can achieve 50-60% recall with rule-based improvements
- Cost-effective (free vs paid services)

---

## Status: ?? **DEFERRED** (Awaiting Scope Confirmation)

**Current State**: Phase 11 complete, baseline measured (2% recall)

**Recommended Next Steps**:
1. ? Confirm Phase 12 will focus on baseline enhancement (Option A)
2. ? Implement proper noun dictionary
3. ? Enhance multi-word detection
4. ? Add relationship patterns
5. ? Test and measure improvements

**Alternative**: Skip Phase 12 and proceed to Phase 13 (Advanced Relationships), optimizing baseline incrementally.

---

[? Back to Master Plan](../MasterPlan.md) | [Previous Phase: Knowledge Graph Foundation ?](Phase-11.md) | [Next Phase: Advanced Relationships ?](Phase-13.md)
