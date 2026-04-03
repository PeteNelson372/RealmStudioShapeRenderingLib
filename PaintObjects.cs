/**************************************************************************************************************************
* Copyright 2024, Peter R. Nelson
*
* This file is part of the RealmStudio application. The RealmStudio application is intended
* for creating fantasy maps for gaming and world building.
*
* RealmStudio is free software: you can redistribute it and/or modify it under the terms
* of the GNU General Public License as published by the Free Software Foundation,
* either version 3 of the License, or (at your option) any later version.
*
* This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
* without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
* See the GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License along with this program.
* The text of the GNU General Public License (GPL) is found in the LICENSE.txt file.
* If the LICENSE.txt file is not present or the text of the GNU GPL is not present in the LICENSE.txt file,
* see https://www.gnu.org/licenses/.
*
* For questions about the RealmStudio application or about licensing, please email
* support@brookmonte.com
*
***************************************************************************************************************************/
using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public sealed class PaintObjects
    {
        public readonly static SKPaint CursorCirclePaint = new()
        {
            Color = SKColors.Black,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke,
            //IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash([5F, 5F], 10F),
        };

        public readonly static SKPaint CursorCircleGreenPaint = new()
        {
            Color = SKColors.Green,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke,
            //IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash([5F, 5F], 10F),
        };

        public readonly static SKPaint CursorSquarePaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            Color = SKColors.DarkRed,
            StrokeWidth = 2,
            PathEffect = SKPathEffect.CreateDash([5F, 5F], 10F)
        };

        public readonly static SKPaint MapOutlinePaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            Color = SKColors.Green,
            StrokeWidth = 10,
        };

        public readonly static SKPaint MapBoundaryPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            Color = SKColors.DarkGray,
            IsAntialias = true,
        };

        public readonly static SKPaint LandformSelectPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            Color = SKColors.Blue,
            StrokeWidth = 2,
            PathEffect = SKPathEffect.CreateDash([5F, 5F], 10F),
        };

        public readonly static SKPaint LandBaseFillPaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public readonly static SKPaint MaskFillPaint = new()
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.White,
        };

        public readonly static SKPaint LandColorPaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public readonly static SKPaint LandColorEraserPaint = new()
        {
            Color = SKColors.Transparent,
            Style = SKPaintStyle.Fill,
            BlendMode = SKBlendMode.Src,
            IsAntialias = false,
        };

        public readonly static SKPaint InteriorFloorColorPaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public readonly static SKPaint InteriorFloorColorEraserPaint = new()
        {
            Color = SKColors.Transparent,
            Style = SKPaintStyle.Fill,
            BlendMode = SKBlendMode.Src,
            IsAntialias = false,
        };

        public readonly static SKPaint InteriorFloorSelectPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            Color = SKColors.Coral,
            StrokeWidth = 2,
            PathEffect = SKPathEffect.CreateDash([5F, 5F], 10F),
        };

        public readonly static SKPaint LandformAreaSelectPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            Color = SKColors.Green,
            StrokeWidth = 3,
            PathEffect = SKPathEffect.CreateDash([4F, 4F], 8F),
        };

        public static SKPaint WaterFeatureSelectPaint { get; } = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            Color = SKColors.CadetBlue,
            StrokeWidth = 2,
            PathEffect = SKPathEffect.CreateDash([3F, 3F], 6F)
        };

        public static SKPaint WaterColorPaint { get; set; } = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            BlendMode = SKBlendMode.SrcOver
        };

        public static SKPaint WaterColorEraserPaint { get; } = new()
        {
            Color = SKColor.Empty,
            Style = SKPaintStyle.Fill,
            BlendMode = SKBlendMode.Src,
            IsAntialias = true
        };

        public readonly static SKPaint CursorCircleStrokePaint = new()
        {
            Color = SKColors.Black,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke,
            //IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash([5F, 5F], 10F),
        };

        public readonly static SKPaint CursorCircleFillPaint = new()
        {
            Color = SKColors.Empty,
            Style = SKPaintStyle.Fill,
        };

        public readonly static SKPaint ContourPathPaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = false,
            Color = SKColors.Black,
            StrokeWidth = 1,
        };

        public readonly static SKPaint ContourMarginPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = false,
            Color = SKColors.White,
            StrokeWidth = 2,
        };

        // the SKPaint object used to draw the box around the selected symbol
        public readonly static SKPaint MapSymbolSelectPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            Color = SKColors.LawnGreen,
            StrokeWidth = 1,
            PathEffect = SKPathEffect.CreateDash([3F, 3F], 6F),
        };

        public readonly static SKPaint RiverSelectPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            Color = SKColors.BlueViolet,
            StrokeWidth = 2,
            PathEffect = SKPathEffect.CreateDash([5F, 5F], 10F)
        };

        public readonly static SKPaint RiverControlPointPaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            StrokeWidth = 2,
            Color = SKColors.WhiteSmoke
        };

        public readonly static SKPaint RiverControlPointOutlinePaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeWidth = 1,
            Color = SKColors.Black
        };

        public readonly static SKPaint RiverSelectedControlPointPaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            StrokeWidth = 2,
            Color = SKColors.BlueViolet,
        };

        public readonly static SKPaint MapPathSelectPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            Color = SKColors.BlueViolet,
            StrokeWidth = 2,
            PathEffect = SKPathEffect.CreateDash([5F, 5F], 10F)
        };

        public readonly static SKPaint MapPathSelectErasePaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            Color = SKColors.Empty,
            StrokeWidth = 2,
        };

        public readonly static SKPaint MapPathControlPointPaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            StrokeWidth = 2,
            Color = SKColors.WhiteSmoke,
        };

        public readonly static SKPaint MapPathSelectedControlPointPaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            Color = SKColors.BlueViolet,
        };

        public readonly static SKPaint MapPathControlPointOutlinePaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeWidth = 1,
            Color = SKColors.Black,
        };

        public readonly static SKPaint MapPathDoubleLinePaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
        };

        public readonly static SKPaint DashPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
        };

        public readonly static SKPaint LabelSelectPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            Color = SKColors.Coral,
            StrokeWidth = 1,
            PathEffect = SKPathEffect.CreateDash([4F, 2F], 6F),
        };

        public readonly static SKPaint LabelPathPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            Color = SKColors.Gray,
            StrokeWidth = 1,
            PathEffect = SKPathEffect.CreateDash([2F, 2F], 4F)
        };

        public readonly static SKPaint RegionSelectPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            Color = SKColors.BlueViolet,
            StrokeWidth = 2,
            PathEffect = SKPathEffect.CreateDash([5F, 5F], 10F),
        };

        public readonly static SKPaint RegionPointFillPaint = new()
        {
            Style = SKPaintStyle.StrokeAndFill,
            IsAntialias = true,
            Color = SKColors.White,
            StrokeWidth = 1,
        };

        public readonly static SKPaint RegionPointSelectedFillPaint = new()
        {
            Style = SKPaintStyle.StrokeAndFill,
            IsAntialias = true,
            Color = SKColors.Blue,
            StrokeWidth = 1,
        };

        public readonly static SKPaint RegionNewPointFillPaint = new()
        {
            Style = SKPaintStyle.StrokeAndFill,
            IsAntialias = true,
            Color = SKColors.Yellow,
            StrokeWidth = 1,
        };

        public readonly static SKPaint RegionPointOutlinePaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            Color = SKColors.Black,
            StrokeWidth = 1,
        };

        public readonly static SKPaint OceanPaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            BlendMode = SKBlendMode.SrcOver,
        };

        public readonly static SKPaint OceanEraserPaint = new()
        {
            Color = SKColors.Empty,
            Style = SKPaintStyle.Fill,
            BlendMode = SKBlendMode.Src
        };

        public readonly static SKPaint BoxPaint = new();

        public readonly static SKPaint BoxSelectPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            Color = SKColors.Green,
            StrokeWidth = 3,
            PathEffect = SKPathEffect.CreateDash([4F, 2F], 6F),
        };

        public readonly static SKPaint SelectedDrawnObjectPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            Color = SKColors.BlueViolet,
            StrokeWidth = 2,
            PathEffect = SKPathEffect.CreateDash([5F, 5F], 10F),
        };

        public readonly static SKPaint LandformRenderFastPaint = new()
        {
            Style = SKPaintStyle.Fill,
            BlendMode = SKBlendMode.Multiply,
            IsAntialias = true
        };

        public readonly static SKPaint LandformRippleRingPaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        public readonly static SKPaint CoastlineBasePaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };

        public readonly static SKPaint LandformInteriorShadingPaint = new()
        {
            BlendMode = SKBlendMode.Multiply,
            IsAntialias = true
        };

        public readonly static SKPaint LandformInteriorGradientPaint = new()
        {
            Style = SKPaintStyle.Fill,
            BlendMode = SKBlendMode.Multiply,
            IsAntialias = true
        };

        public readonly static SKPaint Shape2DSelectPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.DeepSkyBlue,
            StrokeWidth = 2,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash([10, 6], 0)
        };

        public readonly static SKPaint WaterSystemSelectPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Crimson,
            StrokeWidth = 2,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash([10, 6], 0)
        };

        public readonly static SKPaint TransformHandlePaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            Color = SKColors.Cyan,
        };

        public readonly static SKPaint TransformHandleOutlinePaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeWidth = 1,
            Color = SKColors.Black,
        };

        public readonly static SKPaint TransformRotatePaint = new()
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Gold,
            IsAntialias = true
        };

        public readonly static SKPaint TransformHandleHoverFillPaint = new()
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.BlueViolet,
            IsAntialias = true
        };

        public readonly static SKPaint TransformHandleHoverStrokePaint = new()
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            Color = SKColors.Black,
            IsAntialias = true
        };

        public readonly static SKPaint TransformRotateHoverPaint = new()
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Orange,
            IsAntialias = true
        };

        public readonly static SKPaint DebugPaint = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = false,
            Color = SKColors.Crimson,
            StrokeWidth = 2,
        };

        public readonly static SKPaint DebugPaint2 = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = false,
            Color = SKColors.Red,
            StrokeWidth = 5,
            PathEffect = SKPathEffect.CreateDash([4F, 2F], 6F),
        };

        public readonly static SKPaint DebugPaint3 = new()
        {
            Style = SKPaintStyle.Stroke,
            IsAntialias = false,
            Color = SKColors.LimeGreen,
            StrokeWidth = 6,
            PathEffect = SKPathEffect.CreateDash([10, 6], 0)
        };


    }
       
}
