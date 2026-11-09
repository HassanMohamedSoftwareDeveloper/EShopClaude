---
name: auto-spec
description: Run one microservice spec end to end by sequencing /new-spec, /start-spec and /finish-spec, with a human gate after the spec is drafted and after the plan is made. Repeats none of their steps; stops on the first refusal.
argument-hint: <service> <NN>
disable-model-invocation: true
---

# /auto-spec

Arguments: `$ARGUMENTS` → `<service> <NN>`. If either is missing, ask for it and stop.

This skill only sequences. Each step's procedure lives in its own skill and is not restated here.

## How to run a skill
The three skills set `disable-model-invocation: true`, so the Skill tool cannot call them. To run `/<name>`:
1. Read `.claude/skills/<name>/SKILL.md` and follow it exactly, with `$ARGUMENTS` = `<service> <NN>`.
2. Where the file has an injected-state line (`` !`<command>` ``), run that command at that point and use its output.

## Sequence
1. Run **/new-spec**.
2. **Gate one.** Show the completed spec file in full. Ask with AskUserQuestion: "Is this spec buildable as written?" (options **Yes, start it** / **No, stop**). Anything but Yes → stop.
3. Run **/start-spec**. It ends by presenting the plan with ExitPlanMode.
4. **Gate two.** That ExitPlanMode approval is the gate. If the plan is rejected → stop.
5. Run **/finish-spec**. Its own confirm-before-merge prompt still applies.
6. Print /finish-spec's step 8 report as the close-out summary.

## Refusals
A refusal is any failed check, "stop", or "no" inside one of the three skills, or a "no" at a gate. On a refusal:
- Stop immediately. Run none of the later steps.
- Report: which skill (or gate) refused, the check or question that failed, its evidence, and its "what to do next".
- Never work around it. Don't fix the cause yourself, skip the check, or rerun with changed inputs.

## When not to use this
Run the three skills by hand, with a person reviewing between each one, when:
- the spec introduces a pattern this repository has not built before;
- the spec touches more than one service;
- the spec's Definition of Done contains a check nobody can actually run.
