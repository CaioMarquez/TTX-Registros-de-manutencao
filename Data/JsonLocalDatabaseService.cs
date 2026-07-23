using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TTXEquipamentos.Services;
using TTXEquipamentos.Converters;
using TTXEquipamentos.Models;

namespace TTXEquipamentos.Data
{
    /// <summary>
    /// Serviço de banco de dados JSON otimizado para performance
    /// - Usa System.Text.Json ao invés de Newtonsoft (2x mais rápido)
    /// - Implementa lazy loading para reduzir uso de memória
    /// - Carrega apenas dados críticos na inicialização
    /// </summary>
    public class JsonLocalDatabaseService : ILocalDatabaseService
    {
        private readonly string _dataDirectory;
        private readonly object _lockObj = new object();
        // Cache apenas para dados pequenos/críticos
        private Dictionary<string, List<Dictionary<string, object>>> _cache = new();
        
        private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
        
        private static JsonSerializerOptions CreateJsonOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true
            };
            
            // Add case-insensitive enum converters
            options.Converters.Add(new CaseInsensitiveEnumConverter<MaintenanceType>());
            options.Converters.Add(new CaseInsensitiveEnumConverter<MaintenanceNature>());
            options.Converters.Add(new CaseInsensitiveEnumConverter<AppRole>());
            options.Converters.Add(new CaseInsensitiveEnumConverter<ChecklistStatus>());
            
            return options;
        }

        public JsonLocalDatabaseService()
        {
            _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ttx-dados");
            Serilog.Log.Information($"[JsonLocalDatabaseService] BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
            Serilog.Log.Information($"[JsonLocalDatabaseService] DataDirectory: {_dataDirectory}");
        }

        public void Initialize()
        {
            lock (_lockObj)
            {
                // Create data directory if not exists
                if (!Directory.Exists(_dataDirectory))
                {
                    Serilog.Log.Warning($"[JsonLocalDatabaseService] Diretório não encontrado, criando: {_dataDirectory}");
                    Directory.CreateDirectory(_dataDirectory);
                }
                Serilog.Log.Information($"[JsonLocalDatabaseService] Diretório de dados confirmado: {_dataDirectory}");

                // Initialize seed data files if they don't exist
                InitializeSeedData();

                // Load all data into cache
                LoadAllData();
            }
        }

        private void InitializeSeedData()
        {
            // profiles.json
            var profilesPath = Path.Combine(_dataDirectory, "profiles.json");
            if (!File.Exists(profilesPath))
            {
                var masterProfile = new
                {
                    id = "user_1",
                    email = "suporte.master@ttx.com.br",
                    password = "TTX@Master.2025!",
                    name = "Master Admin",
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now
                };

                var profiles = new[] { masterProfile };
                SaveJsonFile(profilesPath, profiles);
            }

            // user_roles.json
            var rolesPath = Path.Combine(_dataDirectory, "user_roles.json");
            if (!File.Exists(rolesPath))
            {
                var roles = new[]
                {
                    new { user_id = "user_1", role = "admin" }
                };
                SaveJsonFile(rolesPath, roles);
            }

            // machines.json
            var machinesPath = Path.Combine(_dataDirectory, "machines.json");
            if (!File.Exists(machinesPath))
            {
                var machines = GenerateDefaultMachines();
                SaveJsonFile(machinesPath, machines);
            }

            // Other required files
            var requiredFiles = new[]
            {
                "checklist_templates.json",
                "contractors.json",
                "email_settings.json",
                "extra_costs.json",
                "maintenance_items.json",
                "maintenance_plan.json",
                "maintenance_records.json",
                "requesters.json",
                "system_diagnostics.json"
            };

            foreach (var file in requiredFiles)
            {
                var filePath = Path.Combine(_dataDirectory, file);
                if (!File.Exists(filePath))
                {
                    SaveJsonFile(filePath, new object[0]);
                }
            }
        }

        private object[] GenerateDefaultMachines()
        {
            var machines = new List<object>();
            var areas = new[] { "Caldeiraria", "Solda", "Acabamento", "Usinagem", "Montagem" };
            var types = new[] { "Elétrica", "Mecânica" };

            for (int i = 1; i <= 70; i++)
            {
                machines.Add(new
                {
                    id = $"machine_{i}",
                    tag = $"MQ-{i:D3}",
                    name = $"Máquina {i}",
                    area = areas[i % areas.Length],
                    type = types[i % types.Length],
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now
                });
            }

            return machines.ToArray();
        }

        private void LoadAllData()
        {
            _cache.Clear();

            // OTIMIZAÇÃO: Carregar APENAS dados críticos na inicialização
            // Dados grandes (maintenance_records) são carregados sob demanda (lazy loading)
            var criticalFiles = new[] { "profiles", "user_roles", "machines" };

            foreach (var fileName in criticalFiles)
            {
                var path = Path.Combine(_dataDirectory, $"{fileName}.json");
                if (File.Exists(path))
                {
                    try
                    {
                        var json = File.ReadAllText(path);
                        var data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json, JsonOptions) ?? new();
                        _cache[fileName] = data;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading {fileName}: {ex.Message}");
                        _cache[fileName] = new();
                    }
                }
            }
        }

        private void SaveJsonFile(string path, object data)
        {
            try
            {
                // OTIMIZAÇÃO: System.Text.Json é 2x mais rápido que Newtonsoft.Json
                var json = JsonSerializer.Serialize(data, JsonOptions);
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Atomic write: escrever em arquivo temporário primeiro
                var tempPath = path + ".tmp";
                File.WriteAllText(tempPath, json);
                
                // Substituir arquivo original atomicamente
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.Move(tempPath, path, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] SaveJsonFile - Failed to save {path}: {ex.Message}");
                throw;
            }
        }

        private string NormalizeJsonForEntity(string json, string entityType)
        {
            if (!string.Equals(entityType, "maintenance_records", StringComparison.OrdinalIgnoreCase))
            {
                return json;
            }

            JsonArray? NormalizeToReportItems(JsonNode? node)
            {
                if (node is null)
                {
                    return new JsonArray();
                }

                if (node is JsonArray array)
                {
                    var normalized = new JsonArray();
                    foreach (var element in array)
                    {
                        if (element is JsonObject reportItem)
                        {
                            normalized.Add(reportItem);
                            continue;
                        }

                        try
                        {
                            var text = element?.GetValue<string>();
                            if (!string.IsNullOrEmpty(text))
                            {
                                normalized.Add(new JsonObject { ["description"] = text });
                                continue;
                            }
                        }
                        catch
                        {
                            // fall through and preserve the element as-is if it is not a string
                        }

                        normalized.Add(element);
                    }
                    return normalized;
                }

                if (node is JsonObject reportObject)
                {
                    return new JsonArray { reportObject };
                }

                try
                {
                    var text = node.GetValue<string>();
                    if (!string.IsNullOrEmpty(text))
                    {
                        return new JsonArray { new JsonObject { ["description"] = text } };
                    }
                }
                catch
                {
                    // ignore
                }

                // Return empty array for null, empty, or unparseable values
                return new JsonArray();
            }

            try
            {
                var node = JsonNode.Parse(json);
                if (node is not JsonArray array)
                {
                    return json;
                }

                foreach (var itemNode in array.OfType<JsonObject>())
                {
                    if (itemNode.ContainsKey("cause_root") && !itemNode.ContainsKey("root_cause"))
                    {
                        itemNode["root_cause"] = itemNode["cause_root"];
                    }

                    if (itemNode.ContainsKey("activities") && !itemNode.ContainsKey("falha_aparente"))
                    {
                        itemNode["falha_aparente"] = itemNode["activities"];
                    }

                    if (itemNode.ContainsKey("incidents") && !itemNode.ContainsKey("trabalho_executado"))
                    {
                        itemNode["trabalho_executado"] = itemNode["incidents"];
                    }

                    if (itemNode.ContainsKey("comments") && !itemNode.ContainsKey("comentarios"))
                    {
                        itemNode["comentarios"] = itemNode["comments"];
                    }

                    if (itemNode.ContainsKey("observations") && !itemNode.ContainsKey("observacoes"))
                    {
                        itemNode["observacoes"] = itemNode["observations"];
                    }

                    if (itemNode.ContainsKey("items") && !itemNode.ContainsKey("itens_utilizados"))
                    {
                        itemNode["itens_utilizados"] = itemNode["items"];
                    }

                    if (itemNode.ContainsKey("root_cause") && (!itemNode.ContainsKey("falha_aparente") || (itemNode["falha_aparente"] is JsonArray arr && arr.Count == 0)))
                    {
                        itemNode["falha_aparente"] = itemNode["root_cause"];
                    }

                    if (itemNode.ContainsKey("description") && !itemNode.ContainsKey("comentarios"))
                    {
                        itemNode["comentarios"] = itemNode["description"];
                    }

                    if (itemNode.ContainsKey("work_description") && !itemNode.ContainsKey("trabalho_executado"))
                    {
                        itemNode["trabalho_executado"] = itemNode["work_description"];
                    }

                    if (itemNode.ContainsKey("falha_aparente"))
                    {
                        var nodeToParse = itemNode["falha_aparente"];
                        var cloned = JsonNode.Parse(nodeToParse?.ToJsonString() ?? "null");
                        var normalized = NormalizeToReportItems(cloned);
                        itemNode["falha_aparente"] = normalized;
                    }

                    if (itemNode.ContainsKey("trabalho_executado"))
                    {
                        var nodeToParse = itemNode["trabalho_executado"];
                        var cloned = JsonNode.Parse(nodeToParse?.ToJsonString() ?? "null");
                        var normalized = NormalizeToReportItems(cloned);
                        itemNode["trabalho_executado"] = normalized;
                    }

                    if (itemNode.ContainsKey("comentarios"))
                    {
                        var nodeToParse = itemNode["comentarios"];
                        var cloned = JsonNode.Parse(nodeToParse?.ToJsonString() ?? "null");
                        var normalized = NormalizeToReportItems(cloned);
                        itemNode["comentarios"] = normalized;
                    }

                    if (itemNode.ContainsKey("checklist_items"))
                    {
                        if (itemNode["checklist_items"] is JsonArray checklistItemsArray)
                        {
                            var normalizedChecklistItems = new JsonArray();
                            foreach (var checklistItemNode in checklistItemsArray.OfType<JsonObject>())
                            {
                                var description = checklistItemNode["description"]?.GetValue<string>()
                                    ?? checklistItemNode["item"]?.GetValue<string>()
                                    ?? checklistItemNode["name"]?.GetValue<string>();

                                var notes = checklistItemNode["notes"]?.GetValue<string>()
                                    ?? checklistItemNode["defect"]?.GetValue<string>()
                                    ?? checklistItemNode["observations"]?.GetValue<string>();

                                var status = checklistItemNode["status"]?.GetValue<string>()?.Trim().ToLowerInvariant();
                                var normalizedStatus = status switch
                                {
                                    "ok" => "ok",
                                    "nao_ok" => "nao_ok",
                                    "na" => "na",
                                    "não_ok" => "nao_ok",
                                    "n/a" => "na",
                                    _ => "na"
                                };

                                normalizedChecklistItems.Add(new JsonObject
                                {
                                    ["id"] = checklistItemNode["id"]?.GetValue<string>() ?? Guid.NewGuid().ToString(),
                                    ["description"] = description,
                                    ["status"] = normalizedStatus,
                                    ["notes"] = notes
                                });
                            }

                            itemNode["checklist_items"] = normalizedChecklistItems;
                        }
                    }
                    else if (itemNode.ContainsKey("checklist_responses"))
                    {
                        var responses = itemNode["checklist_responses"];
                        if (responses is JsonArray responsesArray)
                        {
                            var checklistItems = new JsonArray();
                            foreach (var responseNode in responsesArray.OfType<JsonObject>())
                            {
                                var description = responseNode["item"]?.GetValue<string>()
                                    ?? responseNode["description"]?.GetValue<string>();

                                var status = responseNode["status"]?.GetValue<string>()?.Trim().ToLowerInvariant();
                                var normalizedStatus = status switch
                                {
                                    "ok" => "ok",
                                    "nao_ok" => "nao_ok",
                                    "na" => "na",
                                    "não_ok" => "nao_ok",
                                    "n/a" => "na",
                                    _ => "na"
                                };

                                checklistItems.Add(new JsonObject
                                {
                                    ["id"] = Guid.NewGuid().ToString(),
                                    ["description"] = description,
                                    ["status"] = normalizedStatus,
                                    ["notes"] = responseNode["defect"]?.GetValue<string>()
                                });
                            }

                            itemNode["checklist_items"] = checklistItems;
                        }
                    }

                    itemNode.Remove("cause_root");
                    itemNode.Remove("root_cause");
                    itemNode.Remove("activities");
                    itemNode.Remove("incidents");
                    itemNode.Remove("comments");
                    itemNode.Remove("observations");
                    itemNode.Remove("items");
                    itemNode.Remove("description");
                    itemNode.Remove("work_description");
                    itemNode.Remove("checklist_responses");
                }

                return node.ToJsonString(JsonOptions);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "[JsonLocalDatabaseService] Falha ao normalizar maintenance_records, mantendo JSON original");
                return json;
            }
        }

        private JsonArray NormalizeToReportItems(JsonNode? node)
        {
            if (node is null)
            {
                return new JsonArray();
            }

            if (node is JsonArray array)
            {
                var normalized = new JsonArray();
                foreach (var element in array)
                {
                    if (element is JsonObject reportItem)
                    {
                        normalized.Add(reportItem);
                        continue;
                    }

                    try
                    {
                        var text = element?.GetValue<string>();
                        if (!string.IsNullOrEmpty(text))
                        {
                            normalized.Add(new JsonObject { ["description"] = text });
                            continue;
                        }
                    }
                    catch
                    {
                        // preserve element if it is not a string
                    }

                    normalized.Add(element);
                }
                return normalized;
            }

            if (node is JsonObject reportObject)
            {
                return new JsonArray { reportObject };
            }

            try
            {
                var text = node.GetValue<string>();
                if (!string.IsNullOrEmpty(text))
                {
                    return new JsonArray { new JsonObject { ["description"] = text } };
                }
            }
            catch
            {
                // ignore
            }

            return new JsonArray();
        }

        private void NormalizeMaintenanceRecordItem(JsonObject itemNode)
        {
            if (itemNode.ContainsKey("falha_aparente"))
            {
                var nodeToParse = itemNode["falha_aparente"];
                var cloned = JsonNode.Parse(nodeToParse?.ToJsonString() ?? "null");
                itemNode["falha_aparente"] = NormalizeToReportItems(cloned);
            }

            if (itemNode.ContainsKey("trabalho_executado"))
            {
                var nodeToParse = itemNode["trabalho_executado"];
                var cloned = JsonNode.Parse(nodeToParse?.ToJsonString() ?? "null");
                itemNode["trabalho_executado"] = NormalizeToReportItems(cloned);
            }

            if (itemNode.ContainsKey("comentarios"))
            {
                var nodeToParse = itemNode["comentarios"];
                var cloned = JsonNode.Parse(nodeToParse?.ToJsonString() ?? "null");
                itemNode["comentarios"] = NormalizeToReportItems(cloned);
            }

            if (itemNode.ContainsKey("checklist_items"))
            {
                if (itemNode["checklist_items"] is JsonArray checklistItemsArray)
                {
                    var normalizedChecklistItems = new JsonArray();
                    foreach (var checklistItemNode in checklistItemsArray.OfType<JsonObject>())
                    {
                        var description = checklistItemNode["description"]?.GetValue<string>()
                            ?? checklistItemNode["item"]?.GetValue<string>()
                            ?? checklistItemNode["name"]?.GetValue<string>();

                        var notes = checklistItemNode["notes"]?.GetValue<string>()
                            ?? checklistItemNode["defect"]?.GetValue<string>()
                            ?? checklistItemNode["observations"]?.GetValue<string>();

                        var status = checklistItemNode["status"]?.GetValue<string>()?.Trim().ToLowerInvariant();
                        var normalizedStatus = status switch
                        {
                            "ok" => "ok",
                            "nao_ok" => "nao_ok",
                            "na" => "na",
                            "não_ok" => "nao_ok",
                            "n/a" => "na",
                            _ => "na"
                        };

                        normalizedChecklistItems.Add(new JsonObject
                        {
                            ["id"] = checklistItemNode["id"]?.GetValue<string>() ?? Guid.NewGuid().ToString(),
                            ["description"] = description,
                            ["status"] = normalizedStatus,
                            ["notes"] = notes
                        });
                    }

                    itemNode["checklist_items"] = normalizedChecklistItems;
                }
            }
            else if (itemNode.ContainsKey("checklist_responses"))
            {
                var responses = itemNode["checklist_responses"];
                if (responses is JsonArray responsesArray)
                {
                    var checklistItems = new JsonArray();
                    foreach (var responseNode in responsesArray.OfType<JsonObject>())
                    {
                        var description = responseNode["item"]?.GetValue<string>()
                            ?? responseNode["description"]?.GetValue<string>();

                        var status = responseNode["status"]?.GetValue<string>()?.Trim().ToLowerInvariant();
                        var normalizedStatus = status switch
                        {
                            "ok" => "ok",
                            "nao_ok" => "nao_ok",
                            "na" => "na",
                            "não_ok" => "nao_ok",
                            "n/a" => "na",
                            _ => "na"
                        };

                        checklistItems.Add(new JsonObject
                        {
                            ["id"] = Guid.NewGuid().ToString(),
                            ["description"] = description,
                            ["status"] = normalizedStatus,
                            ["notes"] = responseNode["defect"]?.GetValue<string>()
                        });
                    }

                    itemNode["checklist_items"] = checklistItems;
                }
            }
        }

        public Task<List<T>> GetAllAsync<T>(string entityType) where T : class
        {
            // OTIMIZAÇÃO: Verificar cache primeiro
            lock (_lockObj)
            {
                if (_cache.TryGetValue(entityType, out var data))
                {
                    var results = new List<T>();
                    foreach (var item in data)
                    {
                        try
                        {
                            // System.Text.Json: converter dicionário para tipo T
                            var json = JsonSerializer.Serialize(item, JsonOptions);
                            var obj = JsonSerializer.Deserialize<T>(json, JsonOptions);
                            if (obj != null) results.Add(obj);
                        }
                        catch (Exception ex) 
                        { 
                            System.Diagnostics.Debug.WriteLine($"[JsonLocalDatabaseService] Erro ao desserializar item de {entityType}: {ex.Message}");
                        }
                    }
                    Serilog.Log.Information("[JsonLocalDatabaseService] Carregados {ResultsCount} registros de {EntityType} (from cache)", results.Count, entityType);
                    return Task.FromResult(results);
                }
            }

            // OTIMIZAÇÃO: Lazy loading para dados não cacheados - sem lock para evitar deadlock
            var path = Path.Combine(_dataDirectory, $"{entityType}.json");
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var normalizedJson = NormalizeJsonForEntity(json, entityType);
                    
                    try
                    {
                        // Try standard deserialization first
                        var results = JsonSerializer.Deserialize<List<T>>(normalizedJson, JsonOptions) ?? new();
                        Serilog.Log.Information("[JsonLocalDatabaseService] Carregados {ResultsCount} registros de {EntityType}", results.Count, entityType);
                        return Task.FromResult(results);
                    }
                    catch (JsonException jsonEx)
                    {
                        // If deserialization fails due to bad data, try a fallback approach
                        Serilog.Log.Warning(jsonEx, "[JsonLocalDatabaseService] First deserialization failed, attempting fallback");
                        
                        // Parse as raw objects and filter out bad ones
                        try
                        {
                            var jsonArray = JsonNode.Parse(normalizedJson) as JsonArray;
                            if (jsonArray != null)
                            {
                                var results = new List<T>();
                                foreach (var item in jsonArray)
                                {
                                    try
                                    {
                                        if (string.Equals(entityType, "maintenance_records", StringComparison.OrdinalIgnoreCase)
                                            && item is JsonObject itemNode)
                                        {
                                            NormalizeMaintenanceRecordItem(itemNode);
                                        }

                                        var itemJson = item?.ToJsonString() ?? "{}";
                                        var obj = JsonSerializer.Deserialize<T>(itemJson, JsonOptions);
                                        if (obj != null) results.Add(obj);
                                    }
                                    catch (Exception itemEx)
                                    {
                                        Serilog.Log.Warning(itemEx, "[JsonLocalDatabaseService] Skipped one record due to: {Message}", itemEx.Message);
                                    }
                                }
                                Serilog.Log.Information("[JsonLocalDatabaseService] Carregados {ResultsCount} registros de {EntityType} (fallback)", results.Count, entityType);
                                return Task.FromResult(results);
                            }
                        }
                        catch (Exception fallbackEx)
                        {
                            Serilog.Log.Error(fallbackEx, "[JsonLocalDatabaseService] Fallback failed, returning empty list");
                        }
                        
                        return Task.FromResult(new List<T>());
                    }
                }
                catch (Exception ex) 
                { 
                    System.Diagnostics.Debug.WriteLine($"[JsonLocalDatabaseService] Erro ao carregar {entityType} de {path}: {ex.Message}");
                    Serilog.Log.Error(ex, "[JsonLocalDatabaseService] Erro ao carregar {EntityType}: {ErrorMessage}", entityType, ex.Message);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[JsonLocalDatabaseService] Arquivo não encontrado: {path}");
                Serilog.Log.Information("[JsonLocalDatabaseService] Arquivo não encontrado: {Path}", path);
            }
            return Task.FromResult(new List<T>());
        }

        public async Task<T?> GetByIdAsync<T>(string entityType, string id) where T : class
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    if (_cache.TryGetValue(entityType, out var data))
                    {
                        var item = data.FirstOrDefault(d => 
                        {
                            var hasId = d.TryGetValue("id", out var itemId) || d.TryGetValue("Id", out itemId);
                            return hasId && itemId?.ToString() == id;
                        });
                        
                        if (item != null)
                        {
                            return JsonSerializer.Deserialize<T>(
                                JsonSerializer.Serialize(item, JsonOptions), JsonOptions);
                        }
                    }
                    return null;
                }
            });
        }

        public async Task<bool> SaveAsync<T>(string entityType, T entity) where T : class
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (_lockObj)
                    {
                        // Carrega os dados existentes como tipo correto
                        var path = Path.Combine(_dataDirectory, $"{entityType}.json");
                        List<T> allItems = new();
                        
                        if (File.Exists(path))
                        {
                            try
                            {
                                var existingJson = File.ReadAllText(path);
                                var normalizedJson = NormalizeJsonForEntity(existingJson, entityType);
                                allItems = JsonSerializer.Deserialize<List<T>>(normalizedJson, JsonOptions) ?? new();
                                Serilog.Log.Information("[JsonLocalDatabaseService.SaveAsync] Carregados {ItemCount} registros de {EntityType}", allItems.Count, entityType);
                            }
                            catch (Exception ex)
                            {
                                Serilog.Log.Warning(ex, "[JsonLocalDatabaseService.SaveAsync] Erro ao carregar {EntityType} do arquivo", entityType);
                                allItems = new();
                            }
                        }

                        // Procura pelo ID
                        var idProp = typeof(T).GetProperty("Id", System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public);
                        var entityId = idProp?.GetValue(entity)?.ToString();

                        if (!string.IsNullOrEmpty(entityId))
                        {
                            var existingIndex = allItems.FindIndex(item =>
                            {
                                var itemId = idProp?.GetValue(item)?.ToString();
                                return itemId == entityId;
                            });

                            if (existingIndex >= 0)
                            {
                                allItems[existingIndex] = entity;
                                Serilog.Log.Information("[JsonLocalDatabaseService.SaveAsync] Atualizado registro {EntityId} em {EntityType}", entityId, entityType);
                            }
                            else
                            {
                                allItems.Add(entity);
                                Serilog.Log.Information("[JsonLocalDatabaseService.SaveAsync] Novo registro {EntityId} adicionado a {EntityType}", entityId, entityType);
                            }
                        }
                        else
                        {
                            allItems.Add(entity);
                        }

                        // Persiste para arquivo
                        SaveJsonFile(path, allItems);
                        
                        // Limpa cache para recarregar
                        _cache.Remove(entityType);
                        
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving entity: {ex.Message}");
                    Serilog.Log.Error(ex, "[JsonLocalDatabaseService.SaveAsync] Erro ao salvar {EntityType}: {Error}", entityType, ex.Message);
                    return false;
                }
            });
        }

        public async Task<bool> DeleteAsync<T>(string entityType, string id) where T : class
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (_lockObj)
                    {
                        // Se não está em cache, carregar do arquivo ANTES de tentar deletar
                        if (!_cache.TryGetValue(entityType, out var data))
                        {
                            var path = Path.Combine(_dataDirectory, $"{entityType}.json");
                            if (File.Exists(path))
                            {
                                try
                                {
                                    var existingJson = File.ReadAllText(path);
                                    data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(existingJson, JsonOptions) ?? new();
                                    _cache[entityType] = data;
                                    Serilog.Log.Information("[JsonLocalDatabaseService.DeleteAsync] Carregado cache de {EntityType} do arquivo", entityType);
                                }
                                catch (Exception ex)
                                {
                                    Serilog.Log.Warning(ex, "[JsonLocalDatabaseService.DeleteAsync] Erro ao carregar {EntityType} do arquivo", entityType);
                                    return false;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }

                        var index = data.FindIndex(d =>
                        {
                            var hasId = d.TryGetValue("id", out var itemId) || d.TryGetValue("Id", out itemId);
                            return hasId && itemId?.ToString() == id;
                        });

                        if (index >= 0)
                        {
                            data.RemoveAt(index);

                            // Persistir para arquivo
                            var path2 = Path.Combine(_dataDirectory, $"{entityType}.json");
                            SaveJsonFile(path2, data);
                            return true;
                        }
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting entity: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<Dictionary<string, object>?> GetUserByEmailAsync(string email)
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    if (_cache.TryGetValue("profiles", out var data))
                    {
                        return data.FirstOrDefault(d =>
                            d.TryGetValue("email", out var userEmail) &&
                            userEmail?.ToString() == email);
                    }
                    return null;
                }
            });
        }

        public async Task<string?> GetUserRoleAsync(string userId)
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    if (_cache.TryGetValue("user_roles", out var data))
                    {
                        var roleEntry = data.FirstOrDefault(d =>
                            d.TryGetValue("user_id", out var uid) && uid?.ToString() == userId);

                        return roleEntry?.TryGetValue("role", out var role) == true
                            ? role?.ToString()
                            : null;
                    }
                    return null;
                }
            });
        }

        public async Task<List<Dictionary<string, object>>> GetAllMachinesAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    if (_cache.TryGetValue("machines", out var data))
                    {
                        return data;
                    }

                    // OTIMIZAÇÃO: Lazy loading se não estiver em cache
                    var path = Path.Combine(_dataDirectory, "machines.json");
                    if (File.Exists(path))
                    {
                        try
                        {
                            var json = File.ReadAllText(path);
                            var data2 = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json, JsonOptions);
                            return data2 ?? new();
                        }
                        catch { }
                    }
                    return new();
                }
            });
        }

        public async Task<List<Dictionary<string, object>>> GetAllMaintenanceRecordsAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    if (_cache.TryGetValue("maintenance_records", out var data))
                    {
                        return data;
                    }

                    // OTIMIZAÇÃO: Lazy loading para dados grandes
                    var path = Path.Combine(_dataDirectory, "maintenance_records.json");
                    if (File.Exists(path))
                    {
                        try
                        {
                            var json = File.ReadAllText(path);
                            var data2 = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json, JsonOptions);
                            return data2 ?? new();
                        }
                        catch { }
                    }
                    return new();
                }
            });
        }
    }
}
