using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CustomResearchView;

[HarmonyPatch(typeof(MainTabWindow_Research), "FillTab")]
public static class ResearchViewPatch
{
    private static readonly FieldInfo WindowRectField = AccessTools.Field(typeof(Window), "windowRect");

    public static bool Prefix(MainTabWindow_Research __instance)
    {
        if (WindowRectField == null)
        {
            Log.Error("[CustomResearchView] No se pudo localizar windowRect en Window. Se mantiene la vista original.");
            return true;
        }

        var windowRect = (Rect)WindowRectField.GetValue(__instance);
        var canvas = windowRect.AtZero().ContractedBy(14f);
        ResearchViewDrawer.Draw(canvas);
        return false;
    }
}

internal static class ResearchViewDrawer
{
    private static Vector2 _scrollPosition;
    private static readonly PropertyInfo? ReasonCannotStartNowProperty =
        AccessTools.Property(typeof(ResearchProjectDef), "ReasonCannotStartNow");

    public static void Draw(Rect canvas)
    {
        var previousAnchor = Text.Anchor;
        var previousFont = Text.Font;

        Text.Anchor = TextAnchor.UpperLeft;
        var headerRect = new Rect(canvas.x, canvas.y, canvas.width, 30f);
        Text.Font = GameFont.Medium;
        Widgets.Label(headerRect, "Custom Research Overview".TranslateSimple());
        Text.Font = GameFont.Small;

        var bodyRect = new Rect(canvas.x, canvas.y + 36f, canvas.width, canvas.height - 36f);
        Widgets.DrawMenuSection(bodyRect);

        var inner = bodyRect.ContractedBy(6f);
        var listingWidth = inner.width - 16f;
        var projects = DefDatabase<ResearchProjectDef>.AllDefsListForReading
            .OrderBy(project => project.techLevel)
            .ThenBy(project => project.CostApparent)
            .ToList();

        float contentHeight = GetContentHeight(projects);
        var viewRect = new Rect(0f, 0f, listingWidth, Mathf.Max(contentHeight, inner.height));

        Widgets.BeginScrollView(inner, ref _scrollPosition, viewRect);
        float curY = 0f;
        TechLevel? lastLevel = null;

        foreach (var project in projects)
        {
            if (project.techLevel != lastLevel)
            {
                curY = DrawTechHeader(project.techLevel, viewRect.width, curY);
                lastLevel = project.techLevel;
            }

            curY = DrawProjectRow(project, viewRect.width, curY);
        }

        Widgets.EndScrollView();

        Text.Anchor = previousAnchor;
        Text.Font = previousFont;
    }

    private static float GetContentHeight(List<ResearchProjectDef> projects)
    {
        float height = 0f;
        TechLevel? lastLevel = null;
        foreach (var project in projects)
        {
            if (project.techLevel != lastLevel)
            {
                height += Text.LineHeight * 1.6f;
                lastLevel = project.techLevel;
            }

            height += GetRowHeight(project) + 6f;
        }

        return height + 12f;
    }

    private static float DrawTechHeader(TechLevel level, float width, float curY)
    {
        Rect headerRect = new Rect(0f, curY, width, Text.LineHeight * 1.6f);
        Widgets.DrawLightHighlight(headerRect);
        Text.Font = GameFont.Medium;
        Widgets.Label(headerRect.ContractedBy(4f, 0f), level.GetLabel().CapitalizeFirst());
        Text.Font = GameFont.Small;
        return curY + headerRect.height;
    }

    private static float DrawProjectRow(ResearchProjectDef project, float width, float curY)
    {
        float rowHeight = GetRowHeight(project);
        Rect rowRect = new Rect(0f, curY, width, rowHeight);
        Widgets.DrawHighlightIfMouseover(rowRect);

        var labelRect = new Rect(rowRect.x + 8f, rowRect.y + 4f, width * 0.45f, rowHeight - 8f);
        Widgets.Label(labelRect, project.LabelCap);

        var progressRect = new Rect(labelRect.xMax + 6f, rowRect.y + 6f, width * 0.25f, rowHeight - 12f);
        float progressRaw = project.CostApparent <= 0f
            ? Find.ResearchManager.GetProgress(project)
            : Find.ResearchManager.GetProgress(project) / project.CostApparent;
        float clampedProgress = Mathf.Clamp01(progressRaw);
        Widgets.FillableBar(progressRect, clampedProgress);
        var previousAnchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(progressRect, clampedProgress.ToStringPercent());
        Text.Anchor = previousAnchor;

        var costRect = new Rect(progressRect.xMax + 6f, rowRect.y + 6f, width * 0.1f, rowHeight - 12f);
        Widgets.Label(costRect, ((int)project.CostApparent).ToString());

        var buttonRect = new Rect(rowRect.xMax - 110f, rowRect.y + 4f, 100f, rowHeight - 8f);
        bool canStart = project.CanStartNow;
        bool isActive = Find.ResearchManager.currentProj == project;
        TaggedString buttonLabel = isActive ? "Active".TranslateSimple() : "Research".TranslateSimple();
        bool originalGuiState = GUI.enabled;
        GUI.enabled = canStart && !isActive;
        if (Widgets.ButtonText(buttonRect, buttonLabel))
        {
            Find.ResearchManager.currentProj = project;
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            TaggedString message = "Research project set: {0}".TranslateSimple().Formatted(project.LabelCap);
            Messages.Message(message, MessageTypeDefOf.PositiveEvent);
        }
        GUI.enabled = originalGuiState;

        TooltipHandler.TipRegion(rowRect, new TipSignal(() => GetTooltip(project), project.GetHashCode()));

        return curY + rowHeight + 6f;
    }

    private static float GetRowHeight(ResearchProjectDef project)
    {
        float baseHeight = Text.LineHeight + 12f;
        if (!project.prerequisites.NullOrEmpty())
        {
            baseHeight += Text.LineHeight * 0.8f;
        }

        return baseHeight;
    }

    private static string GetTooltip(ResearchProjectDef project)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine(project.description);
        string reason = GetReasonCannotStart(project);
        if (!reason.NullOrEmpty())
        {
            builder.AppendLine();
            builder.AppendLine("Cannot start:".TranslateSimple());
            builder.AppendLine(reason);
        }

        if (!project.prerequisites.NullOrEmpty())
        {
            builder.AppendLine();
            builder.AppendLine("Prerequisites:".TranslateSimple());
            foreach (var prerequisite in project.prerequisites)
            {
                builder.AppendLine("  • " + prerequisite.LabelCap);
            }
        }

        if (!project.hiddenPrerequisites.NullOrEmpty())
        {
            builder.AppendLine();
            builder.AppendLine("Hidden prerequisites:".TranslateSimple());
            foreach (var hidden in project.hiddenPrerequisites)
            {
                builder.AppendLine("  • " + hidden.LabelCap);
            }
        }

        if (!project.requiredResearchBuilding.NullOrEmpty())
        {
            builder.AppendLine();
            builder.AppendLine("Required research building:".TranslateSimple());
            foreach (var building in project.requiredResearchBuilding)
            {
                builder.AppendLine("  • " + building.LabelCap);
            }
        }

        if (!project.requiredResearchFacilities.NullOrEmpty())
        {
            builder.AppendLine();
            builder.AppendLine("Required facilities:".TranslateSimple());
            foreach (var facility in project.requiredResearchFacilities)
            {
                builder.AppendLine("  • " + facility.LabelCap);
            }
        }

        var unlockedDefs = project.UnlockedDefs()?.ToList();
        if (!unlockedDefs.NullOrEmpty())
        {
            builder.AppendLine();
            builder.AppendLine("Unlocks:".TranslateSimple());
            foreach (var unlock in unlockedDefs!)
            {
                builder.AppendLine("  • " + unlock.LabelCap);
            }
        }

        return builder.ToString();
    }

    private static string GetReasonCannotStart(ResearchProjectDef project)
    {
        if (ReasonCannotStartNowProperty == null)
        {
            return string.Empty;
        }

        return ReasonCannotStartNowProperty.GetValue(project) as string ?? string.Empty;
    }
}
