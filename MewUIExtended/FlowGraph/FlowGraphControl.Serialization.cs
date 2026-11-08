//FlowGraphControl.Serialization.cs
using Aprillz.MewUI;
using MewUIExtended.FlowGraph;
using System.Globalization;
using System.Reflection;
using System.Xml.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MewUI.Extended;

[YamlStaticContext]
[YamlSerializable(typeof(FlowGraphYamlDocument))]
[YamlSerializable(typeof(FlowGraphYamlView))]
[YamlSerializable(typeof(FlowGraphYamlCardType))]
[YamlSerializable(typeof(FlowGraphYamlPortDescriptor))]
[YamlSerializable(typeof(FlowGraphYamlNode))]
[YamlSerializable(typeof(FlowGraphYamlPort))]
[YamlSerializable(typeof(FlowGraphYamlCombo))]
[YamlSerializable(typeof(FlowGraphYamlDualButton))]
[YamlSerializable(typeof(FlowGraphYamlConnection))]
[YamlSerializable(typeof(FlowGraphYamlPoint))]
public partial class FlowGraphStaticContext : StaticContext { }

/// <summary>
/// FlowGraph YAML 导入/导出。
/// YAML 主结构及字段名与 ExDUIR C++ 版保持兼容；额外字段用于保留 C# 版的扩展状态。
/// </summary>
public sealed partial class FlowGraphControl
{
    public event EventHandler? YamlExportRequested;
    public event EventHandler? YamlImportRequested;

    private static ISerializer CreateFlowGraphYamlSerializer()
    {
        return new StaticSerializerBuilder(new FlowGraphStaticContext())
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();
    }

    private static IDeserializer CreateFlowGraphYamlDeserializer()
    {
        return new StaticDeserializerBuilder(new FlowGraphStaticContext())
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>以 C++ FlowGraph YAML 格式导出当前画布。</summary>
    public void ExportYaml(string filePath)
    {
        if (IsExecuting)
            throw new InvalidOperationException("节点执行期间不能导出流程图。");
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("YAML 文件路径不能为空。", nameof(filePath));

        string fullPath = Path.GetFullPath(filePath);
        string directory = Path.GetDirectoryName(fullPath) ?? Environment.CurrentDirectory;
        string baseName = Path.GetFileNameWithoutExtension(fullPath);
        string assetDirectory = Path.Combine(directory, baseName);
        Directory.CreateDirectory(directory);
        Directory.CreateDirectory(assetDirectory);

        var document = new FlowGraphYamlDocument
        {
            View = new FlowGraphYamlView
            {
                Zoom = _zoom,
                PanOffsetX = _panX,
                PanOffsetY = _panY
            }
        };

        foreach (var descriptor in _cardRegistry.Values.OrderBy(d => d.CardType))
            document.CardTypes.Add(ExportCardDescriptor(descriptor));

        foreach (var node in _nodes)
        {
            var yamlNode = new FlowGraphYamlNode
            {
                Id = node.Id,
                CardType = node.CardType,
                DynamicPortCount = node.DynamicPortCount,
                DynamicPortCount2 = node.DynamicPort2Count,
                X = node.X,
                Y = node.Y,
                Width = node.Width,
                Height = node.Height,
                Title = node.Title
            };

            for (int i = 0; i < node.Ports.Count; i++)
                yamlNode.Ports.Add(ExportPort(node, i, node.Ports[i], baseName, assetDirectory));

            document.Nodes.Add(yamlNode);
        }

        foreach (var connection in _connections)
        {
            document.Connections.Add(new FlowGraphYamlConnection
            {
                Id = connection.Id,
                FromNode = connection.FromNodeId,
                FromSlot = connection.FromPortIndex,
                ToNode = connection.ToNodeId,
                ToSlot = connection.ToPortIndex,
                ControlPoint1 = new FlowGraphYamlPoint { X = connection.ControlPoint1.X, Y = connection.ControlPoint1.Y },
                ControlPoint2 = new FlowGraphYamlPoint { X = connection.ControlPoint2.X, Y = connection.ControlPoint2.Y },
                HasCustomControlPoints = connection.HasCustomControlPoints
            });
        }

        try
        {
            var serializer = CreateFlowGraphYamlSerializer();
            //var serializer = new SerializerBuilder()
            //   .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            //   .Build();

            string yaml = serializer.Serialize(document);

            File.WriteAllText(fullPath, yaml, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"FlowGraph YAML 导出失败：{fullPath}", ex);
        }

    }

    /// <summary>导入 C++ FlowGraph YAML 文件，并恢复节点、端口、连线和视图位置。</summary>
    public void ImportYaml(string filePath)
    {
        if (IsExecuting)
            throw new InvalidOperationException("节点执行期间不能导入流程图。");
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("YAML 文件路径不能为空。", nameof(filePath));

        string fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("找不到指定的 YAML 文件。", fullPath);
        try
        {
            var deserializer = CreateFlowGraphYamlDeserializer();
            //var deserializer = new DeserializerBuilder()
            //.IgnoreUnmatchedProperties()
            //.Build();
            var document = deserializer.Deserialize<FlowGraphYamlDocument>(File.ReadAllText(fullPath))
                           ?? throw new InvalidDataException("YAML 文件为空或格式无效。");
            string directory = Path.GetDirectoryName(fullPath) ?? Environment.CurrentDirectory;
            string baseName = Path.GetFileNameWithoutExtension(fullPath);

            // 先恢复未注册的卡片类型；已有描述符不覆盖，以保留当前程序中的 Executor/Tag。
            foreach (var card in document.CardTypes ?? new List<FlowGraphYamlCardType>())
                RegisterDescriptorFromYaml(card);

            Clear();
            var nodeIdMap = new Dictionary<int, int>();

            foreach (var yamlNode in document.Nodes ?? new List<FlowGraphYamlNode>())
            {
                if (yamlNode.CardType <= 0)
                    continue;

                if (!_cardRegistry.ContainsKey(yamlNode.CardType))
                    RegisterFallbackDescriptorFromNode(yamlNode);

                if (!_cardRegistry.ContainsKey(yamlNode.CardType))
                    continue;

                var node = CreateNodeFromCard(yamlNode.CardType, yamlNode.X, yamlNode.Y,
                    string.IsNullOrWhiteSpace(yamlNode.Title) ? null : yamlNode.Title);
                if (yamlNode.Id > 0 && _nodes.All(n => !ReferenceEquals(n, node) && n.Id != yamlNode.Id))
                {
                    node.Id = yamlNode.Id;
                    _nextNodeId = Math.Max(_nextNodeId, node.Id + 1);
                }

                // 使动态端口数量与 YAML 保存值一致（受描述符最小/最大数量约束）。
                int desiredDynamic1 = Math.Max(0, yamlNode.DynamicPortCount);
                int desiredDynamic2 = Math.Max(0, yamlNode.DynamicPortCount2);
                while (node.DynamicPortCount < desiredDynamic1 && AddDynamicPort(node.Id, 1)) { }
                while (node.DynamicPortCount > desiredDynamic1 && RemoveDynamicPort(node.Id, 1)) { }
                while (node.DynamicPort2Count < desiredDynamic2 && AddDynamicPort(node.Id, 2)) { }
                while (node.DynamicPort2Count > desiredDynamic2 && RemoveDynamicPort(node.Id, 2)) { }

                // 与 YAML 端口数量对齐。该操作发生在建立连接之前，不会产生索引偏移问题。
                var yamlPorts = yamlNode.Ports ?? new List<FlowGraphYamlPort>();
                while (node.Ports.Count < yamlPorts.Count)
                {
                    var item = yamlPorts[node.Ports.Count];
                    node.Ports.Add(new FlowGraphPort(item.Name ?? string.Empty,
                        DecodeDirection(item.PortDirection), DecodeDataType(item.DataType)));
                }
                if (node.Ports.Count > yamlPorts.Count && yamlPorts.Count > 0)
                    node.Ports.RemoveRange(yamlPorts.Count, node.Ports.Count - yamlPorts.Count);

                for (int i = 0; i < Math.Min(node.Ports.Count, yamlPorts.Count); i++)
                    ImportPort(node, i, node.Ports[i], yamlPorts[i], directory, baseName);

                node.Title = yamlNode.Title ?? node.Title;
                node.X = yamlNode.X;
                node.Y = yamlNode.Y;
                // 保留 Add/RemoveDynamicPort 实际达到的数量（受描述符 min/max 约束）。

                // 对旧版 YAML 或动态端口描述符不完整的文件，至少确保不会把端口缩放到卡片外。
                FitNodeWidthToWidgets(node);
                RecalculateNodeSize(node);
                node.Width = Math.Max(node.Width, Math.Max(FlowGraphConstants.NodeMinWidth, yamlNode.Width));
                node.Height = Math.Max(node.Height, yamlNode.Height);

                int oldId = yamlNode.Id;
                if (oldId > 0)
                    nodeIdMap[oldId] = node.Id;
            }

            foreach (var yamlConnection in document.Connections ?? new List<FlowGraphYamlConnection>())
            {
                if (!nodeIdMap.TryGetValue(yamlConnection.FromNode, out int fromNodeId) ||
                    !nodeIdMap.TryGetValue(yamlConnection.ToNode, out int toNodeId))
                    continue;

                if (!AddConnection(fromNodeId, yamlConnection.FromSlot, toNodeId, yamlConnection.ToSlot))
                    continue;

                var connection = _connections.LastOrDefault(c =>
                    c.FromNodeId == fromNodeId && c.FromPortIndex == yamlConnection.FromSlot &&
                    c.ToNodeId == toNodeId && c.ToPortIndex == yamlConnection.ToSlot);
                if (connection == null)
                    continue;

                if (yamlConnection.Id > 0 && _connections.All(c =>
                        !ReferenceEquals(c, connection) && c.Id != yamlConnection.Id))
                {
                    connection.Id = yamlConnection.Id;
                    _nextConnectionId = Math.Max(_nextConnectionId, connection.Id + 1);
                }

                if (yamlConnection.ControlPoint1 != null && yamlConnection.ControlPoint2 != null)
                {
                    connection.ControlPoint1 = new Point(yamlConnection.ControlPoint1.X, yamlConnection.ControlPoint1.Y);
                    connection.ControlPoint2 = new Point(yamlConnection.ControlPoint2.X, yamlConnection.ControlPoint2.Y);
                    connection.HasCustomControlPoints = yamlConnection.HasCustomControlPoints ||
                        yamlConnection.ControlPoint1.X != 0 || yamlConnection.ControlPoint1.Y != 0 ||
                        yamlConnection.ControlPoint2.X != 0 || yamlConnection.ControlPoint2.Y != 0;
                }
            }

            // 连接状态以实际连接表为准，避免 YAML 中旧的 is_connected 标志与连接表不一致。
            foreach (var node in _nodes)
            {
                for (int i = 0; i < node.Ports.Count; i++)
                {
                    var port = node.Ports[i];
                    port.IsConnected = port.Direction == FlowGraphPortDirection.Input &&
                        _connections.Any(c => c.ToNodeId == node.Id && c.ToPortIndex == i);
                    AssignPortId(port);
                }
            }

            if (document.View != null)
            {
                _zoom = Math.Clamp(document.View.Zoom <= 0 ? 1.0 : document.View.Zoom,
                    FlowGraphConstants.MinZoom, FlowGraphConstants.MaxZoom);
                _panX = document.View.PanOffsetX;
                _panY = document.View.PanOffsetY;
            }

            _projectName = baseName;
            _selectedNodeIds.Clear();
            _selectedConnectionId = -1;
            RefreshGraph();
            NotifySelectionChanged();
            InvalidateGraphVisual();
        }
        catch(Exception ex)
        {
            throw new InvalidOperationException(
                $"FlowGraph YAML 导入失败：{fullPath}", ex);
        }
        

       
    }

    private FlowGraphYamlCardType ExportCardDescriptor(FlowGraphCardDescriptor descriptor)
    {
        var yaml = new FlowGraphYamlCardType
        {
            CardType = descriptor.CardType,
            TypeName = descriptor.Name,
            TagColor = ColorToArgb(descriptor.HeaderColor),
            DynamicPortBaseIndex = descriptor.DynamicPorts?.BaseIndex ?? -1,
            DynamicPortMinCount = descriptor.DynamicPorts?.MinCount ?? 0,
            DynamicPortMaxCount = descriptor.DynamicPorts?.MaxCount ?? 0,
            DynamicPortDataType = (int)(descriptor.DynamicPorts?.DataType ?? FlowGraphDataType.Any),
            DynamicPortNamePrefix = descriptor.DynamicPorts?.NamePrefix ?? string.Empty,
            DynamicPortDirection = (int)(descriptor.DynamicPorts?.Direction ?? FlowGraphPortDirection.Input),
            DynamicPort2BaseIndex = descriptor.DynamicPorts2?.BaseIndex ?? -1,
            DynamicPort2MinCount = descriptor.DynamicPorts2?.MinCount ?? 0,
            DynamicPort2MaxCount = descriptor.DynamicPorts2?.MaxCount ?? 0,
            DynamicPort2DataType = (int)(descriptor.DynamicPorts2?.DataType ?? FlowGraphDataType.Any),
            DynamicPort2NamePrefix = descriptor.DynamicPorts2?.NamePrefix ?? string.Empty,
            DynamicPort2Direction = (int)(descriptor.DynamicPorts2?.Direction ?? FlowGraphPortDirection.Input)
        };

        foreach (var port in descriptor.Ports)
        {
            yaml.Ports.Add(new FlowGraphYamlPortDescriptor
            {
                PortDirection = (int)port.Direction,
                DirectionExtension = (int)port.Direction,
                DataType = (int)port.DataType,
                Name = port.Name,
                WidgetType = EncodeWidget(port.Widget),
                WidgetKindExtension = (int)port.Widget,
                WidgetId = port.WidgetId,
                WidgetWidth = port.WidgetWidth,
                WidgetHeight = port.WidgetHeight
            });
        }
        return yaml;
    }

    private FlowGraphYamlPort ExportPort(FlowGraphNode node, int index, FlowGraphPort port,
        string baseName, string assetDirectory)
    {
        var yaml = new FlowGraphYamlPort
        {
            Id = port.Id,
            PortDirection = (int)port.Direction,
            DirectionExtension = (int)port.Direction,
            DataType = (int)port.DataType,
            Name = port.Name,
            IsConnected = port.IsConnected,
            WidgetType = EncodeWidget(port.Widget),
            WidgetKindExtension = (int)port.Widget,
            WidgetId = port.WidgetId,
            WidgetWidth = port.WidgetWidth,
            WidgetHeight = port.WidgetHeight,
            DataText = port.Data is FlowGraphImageData ? string.Empty : ToDisplayText(port.Data ?? port.Value)
        };

        string display = port.Data is FlowGraphImageData ? string.Empty : ToDisplayText(port.Data ?? port.Value);
        if (port.Direction == FlowGraphPortDirection.Input && !string.IsNullOrEmpty(display))
            yaml.CachedInputText = display;

        if (port.ComboData != null)
        {
            yaml.ComboData = new FlowGraphYamlCombo
            {
                Current = port.ComboData.Current,
                Options = port.ComboData.Options.ToList()
            };
        }
        else if (port.Widget == FlowGraphWidgetKind.Combo && port.Data is FlowGraphComboData dataCombo)
        {
            yaml.ComboData = new FlowGraphYamlCombo { Current = dataCombo.Current, Options = dataCombo.Options.ToList() };
        }

        if (port.ButtonData != null)
            yaml.ButtonCaption = port.ButtonData.Caption;
        else if (port.Widget == FlowGraphWidgetKind.Button)
            yaml.ButtonCaption = port.Value;

        if (port.DualButtonData != null)
        {
            yaml.DualButtonData = new FlowGraphYamlDualButton
            {
                Caption1 = port.DualButtonData.Caption1,
                Caption2 = port.DualButtonData.Caption2
            };
        }

        if (port.Widget == FlowGraphWidgetKind.Edit)
            yaml.EditText = display;
        else if (port.Widget is FlowGraphWidgetKind.Text or FlowGraphWidgetKind.MultilineText or FlowGraphWidgetKind.Display)
        {
            yaml.TextContent = File.Exists(display)
                ? PackageAsset(display, baseName, assetDirectory, node.Id, port.Id)
                : display;
        }

        // C++ 版仅导出非 OUTPUT 且带 IMAGE 控件的端口。
        // DataType.Image 只表示链路数据类型；普通输入端口的运行时缓存不属于图片预览组件，
        // 不应因此把上游图片再次打包成下游节点的 image_path。
        if (port.Widget == FlowGraphWidgetKind.ImagePreview &&
            port.Direction != FlowGraphPortDirection.Output)
        {
            string imagePath = port.Data is FlowGraphImageData image
                ? image.SourcePath
                : port.ImagePath ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(imagePath))
                yaml.ImagePath = PackageAsset(imagePath, baseName, assetDirectory, node.Id, port.Id);
            if (port.Data is FlowGraphImageData imageData)
            {
                yaml.ImagePixelWidth = imageData.PixelWidth;
                yaml.ImagePixelHeight = imageData.PixelHeight;
            }
        }

        if (port.Widget == FlowGraphWidgetKind.VideoPreview || port.DataType == FlowGraphDataType.Video)
        {
            string videoPath = port.Data as string ?? port.Value;
            if (!string.IsNullOrWhiteSpace(videoPath))
                yaml.VideoPath = PackageAsset(videoPath, baseName, assetDirectory, node.Id, port.Id);
        }

        if (port.DataType == FlowGraphDataType.Audio)
        {
            string audioPath = port.Data as string ?? port.Value;
            if (!string.IsNullOrWhiteSpace(audioPath))
                yaml.AudioPath = PackageAsset(audioPath, baseName, assetDirectory, node.Id, port.Id);
        }

        return yaml;
    }

    private void RegisterDescriptorFromYaml(FlowGraphYamlCardType yaml)
    {
        if (yaml.CardType < 1 || _cardRegistry.ContainsKey(yaml.CardType))
            return;

        var descriptor = new FlowGraphCardDescriptor(yaml.CardType,
            string.IsNullOrWhiteSpace(yaml.TypeName) ? $"Card {yaml.CardType}" : yaml.TypeName)
        {
            HeaderColor = ArgbToColor(yaml.TagColor)
        };

        foreach (var port in yaml.Ports ?? new List<FlowGraphYamlPortDescriptor>())
        {
            descriptor.Ports.Add(new FlowGraphPortDescriptor(port.Name ?? string.Empty,
                DecodeDirection(port.DirectionExtension >= 0 ? port.DirectionExtension : port.PortDirection),
                DecodeDataType(port.DataType))
            {
                Widget = DecodeWidgetExtension(port.WidgetKindExtension, port.WidgetType),
                WidgetId = port.WidgetId,
                WidgetWidth = port.WidgetWidth,
                WidgetHeight = port.WidgetHeight
            });
        }

        if (yaml.DynamicPortBaseIndex >= 0)
        {
            descriptor.DynamicPorts = new FlowGraphDynamicPortGroup(
                yaml.DynamicPortNamePrefix ?? "输入", DecodeDirection(yaml.DynamicPortDirection),
                DecodeDataType(yaml.DynamicPortDataType), yaml.DynamicPortMinCount,
                Math.Max(yaml.DynamicPortMinCount, yaml.DynamicPortMaxCount), yaml.DynamicPortBaseIndex);
        }
        if (yaml.DynamicPort2BaseIndex >= 0)
        {
            descriptor.DynamicPorts2 = new FlowGraphDynamicPortGroup(
                yaml.DynamicPort2NamePrefix ?? "输入", DecodeDirection(yaml.DynamicPort2Direction),
                DecodeDataType(yaml.DynamicPort2DataType), yaml.DynamicPort2MinCount,
                Math.Max(yaml.DynamicPort2MinCount, yaml.DynamicPort2MaxCount), yaml.DynamicPort2BaseIndex);
        }

        RegisterCardType(descriptor);
    }

    private void RegisterFallbackDescriptorFromNode(FlowGraphYamlNode yamlNode)
    {
        if (yamlNode.CardType < 1 || _cardRegistry.ContainsKey(yamlNode.CardType))
            return;

        var descriptor = new FlowGraphCardDescriptor(yamlNode.CardType,
            string.IsNullOrWhiteSpace(yamlNode.Title) ? $"Card {yamlNode.CardType}" : yamlNode.Title);
        foreach (var port in yamlNode.Ports ?? new List<FlowGraphYamlPort>())
        {
            descriptor.Ports.Add(new FlowGraphPortDescriptor(port.Name ?? string.Empty,
                DecodeDirection(port.DirectionExtension >= 0 ? port.DirectionExtension : port.PortDirection),
                DecodeDataType(port.DataType))
            {
                Widget = DecodeWidgetExtension(port.WidgetKindExtension, port.WidgetType),
                WidgetId = port.WidgetId,
                WidgetWidth = port.WidgetWidth,
                WidgetHeight = port.WidgetHeight
            });
        }
        RegisterCardType(descriptor);
    }

    private void ImportPort(FlowGraphNode node, int index, FlowGraphPort port, FlowGraphYamlPort yaml,
        string yamlDirectory, string baseName)
    {
        port.Id = yaml.Id;
        port.Name = yaml.Name ?? port.Name;
        port.Direction = DecodeDirection(yaml.DirectionExtension >= 0 ? yaml.DirectionExtension : yaml.PortDirection,
            port.Direction);
        port.DataType = DecodeDataType(yaml.DataType, port.DataType);
        port.Widget = DecodeWidgetExtension(yaml.WidgetKindExtension, yaml.WidgetType, port.Widget);
        port.WidgetId = yaml.WidgetId;
        port.WidgetWidth = yaml.WidgetWidth;
        port.WidgetHeight = yaml.WidgetHeight > 0 ? yaml.WidgetHeight : port.WidgetHeight;
        port.IsConnected = yaml.IsConnected;
        port.ImagePath = null;
        port.Data = null;

        if (yaml.ComboData != null)
        {
            port.ComboData = new FlowGraphComboData { Current = yaml.ComboData.Current };
            port.ComboData.Options.AddRange(yaml.ComboData.Options ?? new List<string>());
            if (port.ComboData.Options.Count > 0)
            {
                port.ComboData.Current = Math.Clamp(port.ComboData.Current, 0, port.ComboData.Options.Count - 1);
                port.Value = port.ComboData.Options[port.ComboData.Current];
                port.Data = port.Value;
            }
        }
        else if (port.Widget == FlowGraphWidgetKind.Combo)
        {
            port.ComboData ??= new FlowGraphComboData();
        }

        if (yaml.ButtonCaption != null)
            port.ButtonData = new FlowGraphButtonData { Caption = yaml.ButtonCaption };
        else if (port.Widget == FlowGraphWidgetKind.Button)
            port.ButtonData ??= new FlowGraphButtonData { Caption = port.Name };

        if (yaml.DualButtonData != null)
        {
            port.DualButtonData = new FlowGraphDualButtonData
            {
                Caption1 = yaml.DualButtonData.Caption1 ?? "+",
                Caption2 = yaml.DualButtonData.Caption2 ?? "-"
            };
        }
        else if (port.Widget == FlowGraphWidgetKind.DualButton)
            port.DualButtonData ??= new FlowGraphDualButtonData { Caption1 = "+", Caption2 = "-" };

        string? text = yaml.EditText ?? yaml.TextContent ?? yaml.CachedInputText ?? yaml.DataText;
        if (yaml.TextContent != null)
        {
            string maybePath = ResolveAssetPath(yaml.TextContent, yamlDirectory, baseName);
            if (File.Exists(maybePath)) text = maybePath;
        }
        if (yaml.AudioPath != null)
            text = ResolveAssetPath(yaml.AudioPath, yamlDirectory, baseName);
        if (yaml.VideoPath != null)
            text = ResolveAssetPath(yaml.VideoPath, yamlDirectory, baseName);

        if (yaml.ImagePath != null &&
            port.Widget == FlowGraphWidgetKind.ImagePreview &&
            port.Direction != FlowGraphPortDirection.Output)
        {
            string imagePath = ResolveAssetPath(yaml.ImagePath, yamlDirectory, baseName);
            if (File.Exists(imagePath))
            {
                double imageW = yaml.ImagePixelWidth > 0 ? yaml.ImagePixelWidth : 512;
                double imageH = yaml.ImagePixelHeight > 0 ? yaml.ImagePixelHeight : 512;
                var imageData = FlowGraphImageData.FromFile(imagePath, imageW, imageH);
                port.Data = imageData;
                port.ImagePath = imagePath;
            }
        }
        else if (!string.IsNullOrEmpty(text))
        {
            port.Data = text;
        }

        port.Value = port.Data is FlowGraphImageData ? string.Empty : ToDisplayText(port.Data ?? text ?? string.Empty);
        if (port.Widget == FlowGraphWidgetKind.Combo && port.ComboData is { Options.Count: > 0 })
        {
            port.Value = port.ComboData.Options[Math.Clamp(port.ComboData.Current, 0, port.ComboData.Options.Count - 1)];
            port.Data = port.Value;
        }
    }

    private static string PackageAsset(string sourcePath, string baseName, string assetDirectory,
        int nodeId, int portId)
    {
        try
        {
            string fullSource = Path.GetFullPath(sourcePath);
            if (!File.Exists(fullSource))
                return sourcePath;

            string fullAssetDirectory = Path.GetFullPath(assetDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (fullSource.StartsWith(fullAssetDirectory, StringComparison.OrdinalIgnoreCase))
            {
                string relativeInside = Path.GetRelativePath(fullAssetDirectory, fullSource);
                return (baseName + "/" + relativeInside).Replace('\\', '/');
            }

            string fileName = Path.GetFileName(fullSource);
            string destinationName = $"{nodeId}_{portId}_{fileName}";
            string destination = Path.Combine(assetDirectory, destinationName);
            if (!string.Equals(fullSource, Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
                File.Copy(fullSource, destination, overwrite: true);
            return baseName + "/" + destinationName;
        }
        catch
        {
            return sourcePath;
        }
    }

    private static string ResolveAssetPath(string path, string yamlDirectory, string baseName)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;
        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        string normalized = path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        string primary = Path.GetFullPath(Path.Combine(yamlDirectory, normalized));
        if (File.Exists(primary))
            return primary;

        // 与 C++ 版相同：若工程文件夹前缀不存在，再剥离首段尝试从 YAML 所在目录查找。
        string firstSeparator = Path.DirectorySeparatorChar.ToString();
        int separator = normalized.IndexOf(firstSeparator, StringComparison.Ordinal);
        if (separator >= 0)
        {
            string fallback = Path.GetFullPath(Path.Combine(yamlDirectory, normalized[(separator + 1)..]));
            if (File.Exists(fallback))
                return fallback;
        }
        return primary;
    }

    private static int EncodeWidget(FlowGraphWidgetKind widget) => widget switch
    {
        FlowGraphWidgetKind.Edit => 1,
        FlowGraphWidgetKind.Combo => 2,
        FlowGraphWidgetKind.ImagePreview => 3,
        FlowGraphWidgetKind.Button => 4,
        FlowGraphWidgetKind.Text or FlowGraphWidgetKind.MultilineText or FlowGraphWidgetKind.Display => 5,
        FlowGraphWidgetKind.DualButton => 6,
        FlowGraphWidgetKind.VideoPreview => 7,
        _ => 0
    };

    private static FlowGraphWidgetKind DecodeWidget(int widget, FlowGraphWidgetKind fallback = FlowGraphWidgetKind.None)
        => widget switch
        {
            1 => FlowGraphWidgetKind.Edit,
            2 => FlowGraphWidgetKind.Combo,
            3 => FlowGraphWidgetKind.ImagePreview,
            4 => FlowGraphWidgetKind.Button,
            5 => FlowGraphWidgetKind.Text,
            6 => FlowGraphWidgetKind.DualButton,
            7 => FlowGraphWidgetKind.VideoPreview,
            8 => FlowGraphWidgetKind.MultilineText,
            9 => FlowGraphWidgetKind.Display,
            _ => fallback
        };


    private static FlowGraphWidgetKind DecodeWidgetExtension(int extension, int legacyWidget,
    FlowGraphWidgetKind fallback = FlowGraphWidgetKind.None)
    {
        if (extension >= 0)
        {
            return extension switch
            {
                (int)FlowGraphWidgetKind.None => FlowGraphWidgetKind.None,
                (int)FlowGraphWidgetKind.Edit => FlowGraphWidgetKind.Edit,
                (int)FlowGraphWidgetKind.Combo => FlowGraphWidgetKind.Combo,
                (int)FlowGraphWidgetKind.ImagePreview => FlowGraphWidgetKind.ImagePreview,
                (int)FlowGraphWidgetKind.Button => FlowGraphWidgetKind.Button,
                (int)FlowGraphWidgetKind.Text => FlowGraphWidgetKind.Text,
                (int)FlowGraphWidgetKind.MultilineText => FlowGraphWidgetKind.MultilineText,
                (int)FlowGraphWidgetKind.Display => FlowGraphWidgetKind.Display,
                (int)FlowGraphWidgetKind.DualButton => FlowGraphWidgetKind.DualButton,
                (int)FlowGraphWidgetKind.VideoPreview => FlowGraphWidgetKind.VideoPreview,
                _ => DecodeWidget(legacyWidget, fallback)
            };
        }
        return DecodeWidget(legacyWidget, fallback);
    }

    private static FlowGraphPortDirection DecodeDirection(int direction,
        FlowGraphPortDirection fallback = FlowGraphPortDirection.Input)
        => direction switch
        {
            0 => FlowGraphPortDirection.Input,
            1 => FlowGraphPortDirection.Output,
            2 => FlowGraphPortDirection.Inline,
            _ => fallback
        };

    private static FlowGraphDataType DecodeDataType(int dataType, FlowGraphDataType fallback = FlowGraphDataType.Any)
    {
        // 使用 switch 替代 Enum.IsDefined，AOT 安全
        return dataType switch
        {
            (int)FlowGraphDataType.Any => FlowGraphDataType.Any,
            (int)FlowGraphDataType.Image => FlowGraphDataType.Image,
            (int)FlowGraphDataType.Text => FlowGraphDataType.Text,
            (int)FlowGraphDataType.Combo => FlowGraphDataType.Combo,
            (int)FlowGraphDataType.Video => FlowGraphDataType.Video,
            (int)FlowGraphDataType.Audio => FlowGraphDataType.Audio,
            (int)FlowGraphDataType.Number => FlowGraphDataType.Number,
            (int)FlowGraphDataType.Boolean => FlowGraphDataType.Boolean,
            (int)FlowGraphDataType.Object => FlowGraphDataType.Object
           ,
            // ⚠️ 请根据你的实际枚举补全所有值
            _ => fallback
        };
    }

    private static int ColorToArgb(Color color)
    {
        // ⚠️ 假设你使用的是 System.Drawing.Color 或 Avalonia.Media.Color
        // 直接访问属性，彻底移除反射！
        byte a = color.A;
        byte r = color.R;
        byte g = color.G;
        byte b = color.B;

        // 如果你的 Color 是 SkiaSharp.SKColor，请使用 color.Alpha, color.Red 等
        // 如果你的 Color 是 UnityEngine.Color (0~1 float)，请使用 (byte)(color.a * 255) 等

        return unchecked((int)((uint)a << 24 | (uint)r << 16 | (uint)g << 8 | (uint)b));
    }

    private static Color ArgbToColor(int argb)
    {
        uint value = unchecked((uint)argb);
        byte a = (byte)((value >> 24) & 0xFF);
        byte r = (byte)((value >> 16) & 0xFF);
        byte g = (byte)((value >> 8) & 0xFF);
        byte b = (byte)(value & 0xFF);
        if (a == 0) a = 255; // 兼容 C++ 旧文件中没有 Alpha 通道的 RGB 值。
        return Color.FromArgb(a, r, g, b);
    }
}

public sealed class FlowGraphYamlDocument
{
    [YamlMember(Alias = "view")] public FlowGraphYamlView? View { get; set; }
    [YamlMember(Alias = "card_types")] public List<FlowGraphYamlCardType> CardTypes { get; set; }
    [YamlMember(Alias = "nodes")] public List<FlowGraphYamlNode> Nodes { get; set; } 
    [YamlMember(Alias = "connections")] public List<FlowGraphYamlConnection> Connections { get; set; }
    public FlowGraphYamlDocument()
    {
        View = new FlowGraphYamlView();
        CardTypes = new List<FlowGraphYamlCardType>();
        Nodes = new List<FlowGraphYamlNode>();
        Connections = new List<FlowGraphYamlConnection>();
    }
}

public sealed class FlowGraphYamlView
{
    [YamlMember(Alias = "zoom")] public double Zoom { get; set; } 
    [YamlMember(Alias = "pan_offset_x")] public double PanOffsetX { get; set; }
    [YamlMember(Alias = "pan_offset_y")] public double PanOffsetY { get; set; }
    public FlowGraphYamlView()
    {
        Zoom = 1;
    }
}

public sealed class FlowGraphYamlCardType
{
    [YamlMember(Alias = "card_type")] public int CardType { get; set; }
    [YamlMember(Alias = "type_name")] public string? TypeName { get; set; }
    [YamlMember(Alias = "tag_color")] public int TagColor { get; set; }
    [YamlMember(Alias = "dynamic_port_base_index")] public int DynamicPortBaseIndex { get; set; } 
    [YamlMember(Alias = "dynamic_port_min_count")] public int DynamicPortMinCount { get; set; }
    [YamlMember(Alias = "dynamic_port_max_count")] public int DynamicPortMaxCount { get; set; }
    [YamlMember(Alias = "dynamic_port_data_type")] public int DynamicPortDataType { get; set; }
    [YamlMember(Alias = "dynamic_port_name_prefix")] public string? DynamicPortNamePrefix { get; set; }
    [YamlMember(Alias = "dynamic_port_direction")] public int DynamicPortDirection { get; set; }
    [YamlMember(Alias = "dynamic_port2_base_index")] public int DynamicPort2BaseIndex { get; set; } 
    [YamlMember(Alias = "dynamic_port2_min_count")] public int DynamicPort2MinCount { get; set; }
    [YamlMember(Alias = "dynamic_port2_max_count")] public int DynamicPort2MaxCount { get; set; }
    [YamlMember(Alias = "dynamic_port2_data_type")] public int DynamicPort2DataType { get; set; }
    [YamlMember(Alias = "dynamic_port2_name_prefix")] public string? DynamicPort2NamePrefix { get; set; }
    [YamlMember(Alias = "dynamic_port2_direction")] public int DynamicPort2Direction { get; set; }
    [YamlMember(Alias = "ports")] public List<FlowGraphYamlPortDescriptor> Ports { get; set; }
    public FlowGraphYamlCardType()
    {
        DynamicPortBaseIndex = -1;
        DynamicPort2BaseIndex = -1;
        Ports = new List<FlowGraphYamlPortDescriptor>();
    }
}

public sealed class FlowGraphYamlPortDescriptor
{
    [YamlMember(Alias = "port_type")] public int PortDirection { get; set; }
    [YamlMember(Alias = "direction")] public int DirectionExtension { get; set; } 
    [YamlMember(Alias = "data_type")] public int DataType { get; set; }
    [YamlMember(Alias = "name")] public string? Name { get; set; }
    [YamlMember(Alias = "widget_type")] public int WidgetType { get; set; }
    [YamlMember(Alias = "csharp_widget_kind")] public int WidgetKindExtension { get; set; }
    [YamlMember(Alias = "widget_id")] public int WidgetId { get; set; }
    [YamlMember(Alias = "widget_width")] public double WidgetWidth { get; set; }
    [YamlMember(Alias = "widget_height")] public double WidgetHeight { get; set; }
    public FlowGraphYamlPortDescriptor()
    {
        DirectionExtension = -1;
        WidgetKindExtension = -1;
        WidgetHeight = 30;
    }
}

public sealed class FlowGraphYamlNode
{
    [YamlMember(Alias = "id")] public int Id { get; set; }
    [YamlMember(Alias = "card_type")] public int CardType { get; set; }
    [YamlMember(Alias = "dynamic_port_count")] public int DynamicPortCount { get; set; }
    [YamlMember(Alias = "dynamic_port_count2")] public int DynamicPortCount2 { get; set; }
    [YamlMember(Alias = "x")] public double X { get; set; }
    [YamlMember(Alias = "y")] public double Y { get; set; }
    [YamlMember(Alias = "width")] public double Width { get; set; } 
    [YamlMember(Alias = "height")] public double Height { get; set; } 
    [YamlMember(Alias = "title")] public string? Title { get; set; }
    [YamlMember(Alias = "ports")] public List<FlowGraphYamlPort> Ports { get; set; }
    public FlowGraphYamlNode()
    {
        Width = 260;
        Height = 120;
        Ports = new List<FlowGraphYamlPort>();
    }
}

public sealed class FlowGraphYamlPort
{
    [YamlMember(Alias = "id")] public int Id { get; set; }
    [YamlMember(Alias = "port_type")] public int PortDirection { get; set; }
    [YamlMember(Alias = "direction")] public int DirectionExtension { get; set; } 
    [YamlMember(Alias = "data_type")] public int DataType { get; set; }
    [YamlMember(Alias = "name")] public string? Name { get; set; }
    [YamlMember(Alias = "is_connected")] public bool IsConnected { get; set; }
    [YamlMember(Alias = "widget_type")] public int WidgetType { get; set; }
    [YamlMember(Alias = "csharp_widget_kind")] public int WidgetKindExtension { get; set; } 
    [YamlMember(Alias = "widget_id")] public int WidgetId { get; set; }
    [YamlMember(Alias = "widget_width")] public double WidgetWidth { get; set; }
    [YamlMember(Alias = "widget_height")] public double WidgetHeight { get; set; } 
    [YamlMember(Alias = "cached_input_text")] public string? CachedInputText { get; set; }
    [YamlMember(Alias = "combo_data")] public FlowGraphYamlCombo? ComboData { get; set; }
    [YamlMember(Alias = "button_caption")] public string? ButtonCaption { get; set; }
    [YamlMember(Alias = "dual_button_data")] public FlowGraphYamlDualButton? DualButtonData { get; set; }
    [YamlMember(Alias = "edit_text")] public string? EditText { get; set; }
    [YamlMember(Alias = "text_content")] public string? TextContent { get; set; }
    [YamlMember(Alias = "video_path")] public string? VideoPath { get; set; }
    [YamlMember(Alias = "image_path")] public string? ImagePath { get; set; }
    [YamlMember(Alias = "audio_path")] public string? AudioPath { get; set; }
    [YamlMember(Alias = "data_text")] public string? DataText { get; set; }
    [YamlMember(Alias = "image_pixel_width")] public double ImagePixelWidth { get; set; }
    [YamlMember(Alias = "image_pixel_height")] public double ImagePixelHeight { get; set; }
    public FlowGraphYamlPort()
    {
        DirectionExtension = -1;
        WidgetKindExtension = -1;
        WidgetHeight = 30;
    }
}

public sealed class FlowGraphYamlCombo
{
    [YamlMember(Alias = "current")] public int Current { get; set; }
    [YamlMember(Alias = "options")] public List<string> Options { get; set; } 
    public FlowGraphYamlCombo()
    {
        Options = new List<string>();
    }
}

public sealed class FlowGraphYamlDualButton
{
    [YamlMember(Alias = "caption1")] public string? Caption1 { get; set; }
    [YamlMember(Alias = "caption2")] public string? Caption2 { get; set; }
}

public sealed class FlowGraphYamlPoint
{
    [YamlMember(Alias = "x")] public double X { get; set; }
    [YamlMember(Alias = "y")] public double Y { get; set; }
}

public sealed class FlowGraphYamlConnection
{
    [YamlMember(Alias = "id")] public int Id { get; set; }
    [YamlMember(Alias = "from_node")] public int FromNode { get; set; }
    [YamlMember(Alias = "from_slot")] public int FromSlot { get; set; }
    [YamlMember(Alias = "to_node")] public int ToNode { get; set; }
    [YamlMember(Alias = "to_slot")] public int ToSlot { get; set; }
    [YamlMember(Alias = "control_point1")] public FlowGraphYamlPoint? ControlPoint1 { get; set; }
    [YamlMember(Alias = "control_point2")] public FlowGraphYamlPoint? ControlPoint2 { get; set; }
    [YamlMember(Alias = "has_custom_control_points")] public bool HasCustomControlPoints { get; set; }
}
