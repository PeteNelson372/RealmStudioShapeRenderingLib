namespace RealmStudioShapeRenderingLib
{
    public static class CoastlineStyleFactory
    {
        public static ICoastlineStyle Create(
            CoastlineSettings settings,
            IAssetProvider assets)
        {
            switch (settings.CoastlineStyle)
            {
                case LandformCoastlineStyle.None:
                    return new NoCoastlineStyle();

                case LandformCoastlineStyle.UniformBand:
                    return new UniformBandCoastlineStyle(settings);

                case LandformCoastlineStyle.UniformBlend:
                    return new UniformBlendCoastlineStyle(settings);

                case LandformCoastlineStyle.ThreeTiered:
                    return new ThreeTieredCoastlineStyle(settings);

                case LandformCoastlineStyle.HatchPattern:
                    {
                        var image = string.IsNullOrEmpty(settings.HatchTextureId)
                            ? null
                            : assets.GetImage(settings.HatchTextureId);

                        return new HatchCoastlineStyle(settings, image);
                    }

                case LandformCoastlineStyle.DashPattern:
                    {
                        var image = string.IsNullOrEmpty(settings.DashTextureId)
                            ? null
                            : assets.GetImage(settings.DashTextureId);

                        return new DashCoastlineStyle(settings, image);
                    }

                case LandformCoastlineStyle.CircularPattern:
                    {
                        var image = string.IsNullOrEmpty(settings.CircularTextureId)
                            ? null
                            : assets.GetImage(settings.CircularTextureId);

                        return new CircularPatternCoastlineStyle(settings, image);
                    }

                case LandformCoastlineStyle.UserDefined:
                    return new UserDefinedCoastlineStyle(settings);

                default:
                    return new NoCoastlineStyle();
            }
        }
    }


}
