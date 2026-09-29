# 折叠森林美术与交互更新

风格：温暖的绘本森林、奶油色手账界面、柔和绿色与少量金色点缀。

本次加入森林插画底图、有厚度的苔藓地块、石径与花草、森林核心树、三种伙伴的独立几何造型和渲染头像、圆角卡片与按钮、背包容量条、商店头像、萤火虫、魔法粒子、伤害数字、波次提示和合成音效。音效由代码合成，不使用外部音频；右上角可静音。已有玩法规则保持不变。

## 插画素材

文件：`Assets/Resources/ForestBackdrop.png`

通过内置 imagegen 工具生成，未使用 CLI 或 API fallback。已复制到工程 Resources，构建时随游戏打包。

生成提示词：

Use case: stylized-concept. Asset type: landscape 16:9 background plate for a cozy magical forest tabletop tower-defense game, not a screenshot or mockup. Produce one polished hand-painted 2D game environment in soft gouache and premium storybook illustration style. Directly overhead view of a dreamy woodland clearing. Wide empty muted sage/moss green clearing occupying central 75 percent of image with very low contrast texture, no objects in this central play space. Frame far outer margins with rich layered emerald and teal fern fronds, rounded mossy rocks, tiny cream flowers, subtle golden firefly dots, clusters of small warm coral mushrooms and soft forest canopy shadows. Warm gentle dappled afternoon light from upper left, magical peaceful atmosphere, carefully painted leaf shapes and tactile brush textures. Delicate vignette at outer edge, sophisticated natural green and cream palette with restrained amber accents. Must remain very legible as a background behind game tiles. No roads, no tiles, no characters, no buildings, no UI, no lettering, no text, no grid. Landscape composition.

## 可继续替换的资源

伙伴与地块目前是可运行的程序化 3D 美术，头像由对应模型实时渲染。后续可以在保留角色识别特征的前提下替换为手工模型。界面仍由 IMGUI 绘制，当前适合电脑机制 Demo，正式产品可迁移到 UGUI 或 UI Toolkit。
