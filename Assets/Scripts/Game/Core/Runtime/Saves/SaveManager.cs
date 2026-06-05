using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Core.Entities;
using Game.Core.Map;
using Game.Core.Models;
using Game.Core.Random;
using Game.Core.Rooms;
using Game.Core.Rewards;

namespace Game.Core.Saves
{
    /// <summary>
    /// Save manager and binary serializer.
    /// BOUNDARY: StS CardModel/Deck/Encounter serialization removed.
    /// Retained: Map, Room, Player (HP/Gold), RNG state, Act progress.
    /// </summary>
    public sealed class SaveManager
    {
        private readonly string _saveFilePath;

        private static RunSaveDto _cachedRun;

        public SaveManager(string saveDirectory = null)
        {
            _saveFilePath = Path.Combine(
                saveDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TheHollowSketchbook",
                "prototype-current-run.dat");
        }

        public void SaveCurrentRun(Runs.RunState run, Domain.DomainActionContext domain = null)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            _cachedRun = RunSaveSerializer.Capture(run, domain);
            PersistToDisk(_cachedRun);
        }

        public Runs.RunState TryLoadCurrentRun(out RoomDomainStateSaveDto roomDomainState)
        {
            if (_cachedRun == null)
            {
                _cachedRun = LoadFromDisk();
            }

            if (_cachedRun == null)
            {
                roomDomainState = null;
                return null;
            }

            return RunSaveSerializer.Restore(_cachedRun, out roomDomainState);
        }

        public Runs.RunState TryLoadCurrentRun()
        {
            return TryLoadCurrentRun(out _);
        }

        public RunSaveDto ExportCurrentRun()
        {
            return _cachedRun;
        }

        public void ImportCurrentRun(RunSaveDto dto)
        {
            _cachedRun = dto;
            if (dto == null)
            {
                DeleteCurrentRun();
                return;
            }

            PersistToDisk(dto);
        }

        public void DeleteCurrentRun()
        {
            _cachedRun = null;
            if (File.Exists(_saveFilePath))
            {
                File.Delete(_saveFilePath);
            }
        }

        private void PersistToDisk(RunSaveDto dto)
        {
            string directory = Path.GetDirectoryName(_saveFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using FileStream stream = new FileStream(_saveFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8);
            RunSaveBinarySerializer.Write(writer, dto);
        }

        private RunSaveDto LoadFromDisk()
        {
            if (!File.Exists(_saveFilePath))
            {
                return null;
            }

            using FileStream stream = new FileStream(_saveFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using BinaryReader reader = new BinaryReader(stream, Encoding.UTF8);
            return RunSaveBinarySerializer.Read(reader);
        }
    }

    public static class RunSaveBinarySerializer
    {
        public static void Write(BinaryWriter writer, RunSaveDto dto)
        {
            writer.Write(dto.SaveVersion);
            writer.Write(dto.Seed);
            writer.Write(dto.CurrentActIndex);
            writer.Write(dto.IsGameOver);
            WriteNullable(writer, dto.RngState, WriteRngState);
            WriteNullable(writer, dto.CurrentMapCoord, WriteMapCoord);
            WriteNullable(writer, dto.Map, WriteMap);
            WriteNullable(writer, dto.CurrentRoom, WriteRoom);
            WriteNullable(writer, dto.RoomDomainState, WriteRoomDomainState);
            WriteList(writer, dto.Players, WritePlayer);
            WriteList(writer, dto.ActIds, WriteActId);
        }

        public static RunSaveDto Read(BinaryReader reader)
        {
            RunSaveDto dto = new RunSaveDto
            {
                SaveVersion = reader.ReadInt32(),
                Seed = reader.ReadInt32(),
                CurrentActIndex = reader.ReadInt32(),
                IsGameOver = reader.ReadBoolean(),
                RngState = ReadNullable(reader, ReadRngState),
                CurrentMapCoord = ReadNullable(reader, ReadMapCoord),
                Map = ReadNullable(reader, ReadMap),
                CurrentRoom = ReadNullable(reader, ReadRoom)
            };

            if (dto.SaveVersion >= 3)
            {
                dto.RoomDomainState = ReadNullable(reader, r => ReadRoomDomainState(r, dto.SaveVersion));
            }

            dto.Players = ReadList(reader, ReadPlayer);
            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                dto.ActIds = ReadList(reader, ReadActId);
            }

            return dto;
        }

        private static void WritePlayer(BinaryWriter writer, PlayerSaveDto dto)
        {
            writer.Write(dto.CharacterCategory ?? string.Empty);
            writer.Write(dto.CharacterEntry ?? string.Empty);
            writer.Write(dto.CurrentHp);
            writer.Write(dto.MaxHp);
            writer.Write(dto.Gold);
        }

        private static PlayerSaveDto ReadPlayer(BinaryReader reader)
        {
            return new PlayerSaveDto
            {
                CharacterCategory = reader.ReadString(),
                CharacterEntry = reader.ReadString(),
                CurrentHp = reader.ReadInt32(),
                MaxHp = reader.ReadInt32(),
                Gold = reader.ReadInt32()
            };
        }

        private static void WriteMap(BinaryWriter writer, MapSaveDto dto)
        {
            writer.Write(dto.ColumnCount);
            writer.Write(dto.RowCount);
            WriteMapCoord(writer, dto.Start);
            WriteMapCoord(writer, dto.Boss);
            WriteList(writer, dto.Points, WriteMapPoint);
        }

        private static MapSaveDto ReadMap(BinaryReader reader)
        {
            return new MapSaveDto
            {
                ColumnCount = reader.ReadInt32(),
                RowCount = reader.ReadInt32(),
                Start = ReadMapCoord(reader),
                Boss = ReadMapCoord(reader),
                Points = ReadList(reader, ReadMapPoint)
            };
        }

        private static void WriteMapPoint(BinaryWriter writer, MapPointSaveDto dto)
        {
            writer.Write(dto.Column);
            writer.Write(dto.Row);
            writer.Write((int)dto.PointType);
            writer.Write(dto.IsVisited);
            writer.Write(dto.IsCompleted);
            WriteList(writer, dto.Children, WriteMapCoord);
        }

        private static MapPointSaveDto ReadMapPoint(BinaryReader reader)
        {
            return new MapPointSaveDto
            {
                Column = reader.ReadInt32(),
                Row = reader.ReadInt32(),
                PointType = (MapPointType)reader.ReadInt32(),
                IsVisited = reader.ReadBoolean(),
                IsCompleted = reader.ReadBoolean(),
                Children = ReadList(reader, ReadMapCoord)
            };
        }

        private static void WriteMapCoord(BinaryWriter writer, MapCoordSaveDto dto)
        {
            writer.Write(dto.Column);
            writer.Write(dto.Row);
        }

        private static MapCoordSaveDto ReadMapCoord(BinaryReader reader)
        {
            return new MapCoordSaveDto
            {
                Column = reader.ReadInt32(),
                Row = reader.ReadInt32()
            };
        }

        private static void WriteRoom(BinaryWriter writer, RoomSaveDto dto)
        {
            writer.Write((int)dto.RoomType);
            writer.Write(dto.IsCompleted);
            WriteList(writer, dto.Rewards, WriteReward);
        }

        private static RoomSaveDto ReadRoom(BinaryReader reader)
        {
            return new RoomSaveDto
            {
                RoomType = (RoomType)reader.ReadInt32(),
                IsCompleted = reader.ReadBoolean(),
                Rewards = ReadList(reader, ReadReward)
            };
        }

        private static void WriteReward(BinaryWriter writer, RewardSaveDto dto)
        {
            writer.Write((int)dto.RewardType);
            writer.Write(dto.IsResolved);
            writer.Write(dto.GoldAmount);
            writer.Write(dto.SelectedIndex);
            writer.Write(dto.WasSkipped);
            WriteList(writer, dto.ChoiceLabels, (w, s) => w.Write(s ?? string.Empty));
        }

        private static RewardSaveDto ReadReward(BinaryReader reader)
        {
            return new RewardSaveDto
            {
                RewardType = (RewardType)reader.ReadInt32(),
                IsResolved = reader.ReadBoolean(),
                GoldAmount = reader.ReadInt32(),
                SelectedIndex = reader.ReadInt32(),
                WasSkipped = reader.ReadBoolean(),
                ChoiceLabels = ReadList(reader, r => r.ReadString())
            };
        }

        private static void WriteActId(BinaryWriter writer, ActIdSaveDto dto)
        {
            writer.Write(dto.Category ?? string.Empty);
            writer.Write(dto.Entry ?? string.Empty);
        }

        private static ActIdSaveDto ReadActId(BinaryReader reader)
        {
            return new ActIdSaveDto
            {
                Category = reader.ReadString(),
                Entry = reader.ReadString()
            };
        }

        private static void WriteRngState(BinaryWriter writer, RngStateDto dto)
        {
            writer.Write(dto.Value);
        }

        private static RngStateDto ReadRngState(BinaryReader reader)
        {
            return new RngStateDto { Value = reader.ReadUInt32() };
        }

        private static void WriteNullable<T>(BinaryWriter writer, T value, Action<BinaryWriter, T> writeValue)
            where T : class
        {
            bool hasValue = value != null;
            writer.Write(hasValue);
            if (hasValue)
            {
                writeValue(writer, value);
            }
        }

        private static T ReadNullable<T>(BinaryReader reader, Func<BinaryReader, T> readValue)
            where T : class
        {
            return reader.ReadBoolean() ? readValue(reader) : null;
        }

        private static void WriteList<T>(BinaryWriter writer, IReadOnlyList<T> values, Action<BinaryWriter, T> writeValue)
        {
            int count = values != null ? values.Count : 0;
            writer.Write(count);
            for (int i = 0; i < count; i++)
            {
                writeValue(writer, values[i]);
            }
        }

        private static List<T> ReadList<T>(BinaryReader reader, Func<BinaryReader, T> readValue)
        {
            int count = reader.ReadInt32();
            List<T> list = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(readValue(reader));
            }

            return list;
        }

        // --- RoomDomainState serialization ---

        private static void WriteRoomDomainState(BinaryWriter writer, RoomDomainStateSaveDto dto)
        {
            writer.Write(dto.RoomType);
            writer.Write(dto.LayerIndex);
            writer.Write(dto.NodeIndex);
            writer.Write(dto.ActionCounterValue);
            writer.Write(dto.PlayerGold);
            writer.Write(dto.RngState.HasValue);
            if (dto.RngState.HasValue)
            {
                writer.Write(dto.RngState.Value);
            }
            WriteNullable(writer, dto.Grid, WriteGridState);
            WriteNullable(writer, dto.DungeonDeck, WriteDungeonDeck);
            WriteNullable(writer, dto.ItemInventory, WritePlayerInventory);
            WriteNullable(writer, dto.RelicInventory, WriteRelicInventory);
            WriteList(writer, dto.ActiveChoices, WriteChoiceSession);
            WriteList(writer, dto.PendingTriggerCardInstanceIds, (w, id) => w.Write(id));
            WriteList(writer, dto.RouteChoiceRoomTypes, (w, roomType) => w.Write(roomType));
        }

        private static RoomDomainStateSaveDto ReadRoomDomainState(BinaryReader reader, int saveVersion)
        {
            if (saveVersion <= 3)
            {
                return new RoomDomainStateSaveDto
                {
                    ActionCounterValue = reader.ReadInt32(),
                    PlayerGold = reader.ReadInt32(),
                    Grid = ReadNullable(reader, ReadGridState),
                    ItemInventory = ReadNullable(reader, ReadPlayerInventory),
                    RelicInventory = ReadNullable(reader, ReadRelicInventory)
                };
            }

            RoomDomainStateSaveDto dto = new RoomDomainStateSaveDto
            {
                RoomType = reader.ReadInt32(),
                LayerIndex = reader.ReadInt32(),
                NodeIndex = reader.ReadInt32(),
                ActionCounterValue = reader.ReadInt32(),
                PlayerGold = reader.ReadInt32()
            };

            bool hasRngState = reader.ReadBoolean();
            if (hasRngState)
            {
                dto.RngState = reader.ReadUInt32();
            }

            dto.Grid = ReadNullable(reader, ReadGridState);
            dto.DungeonDeck = ReadNullable(reader, ReadDungeonDeck);
            dto.ItemInventory = ReadNullable(reader, ReadPlayerInventory);
            dto.RelicInventory = ReadNullable(reader, ReadRelicInventory);
            dto.ActiveChoices = ReadList(reader, ReadChoiceSession);
            dto.PendingTriggerCardInstanceIds = ReadList(reader, r => r.ReadUInt32());
            dto.RouteChoiceRoomTypes = ReadList(reader, r => r.ReadInt32());
            return dto;
        }

        private static void WriteGridState(BinaryWriter writer, GridStateSaveDto dto)
        {
            WriteList(writer, dto.Cells, WriteGridCell);
            WriteList(writer, dto.Cards, WriteCardInstance);
        }

        private static GridStateSaveDto ReadGridState(BinaryReader reader)
        {
            return new GridStateSaveDto
            {
                Cells = ReadList(reader, ReadGridCell),
                Cards = ReadList(reader, ReadCardInstance)
            };
        }

        private static void WriteGridCell(BinaryWriter writer, GridCellSaveDto dto)
        {
            writer.Write(dto.CoordRow);
            writer.Write(dto.CoordCol);
            WriteList(writer, dto.CardInstanceIds, (w, id) => w.Write(id));
        }

        private static GridCellSaveDto ReadGridCell(BinaryReader reader)
        {
            return new GridCellSaveDto
            {
                CoordRow = reader.ReadInt32(),
                CoordCol = reader.ReadInt32(),
                CardInstanceIds = ReadList(reader, r => r.ReadUInt32())
            };
        }

        private static void WriteCardInstance(BinaryWriter writer, CardInstanceSaveDto dto)
        {
            writer.Write(dto.InstanceId);
            writer.Write(dto.ModelCategory ?? string.Empty);
            writer.Write(dto.ModelEntry ?? string.Empty);
            writer.Write(dto.CardType);
            writer.Write(dto.Zone);
            writer.Write(dto.CoordRow.HasValue);
            if (dto.CoordRow.HasValue)
            {
                writer.Write(dto.CoordRow.Value);
                writer.Write(dto.CoordCol.Value);
            }
            writer.Write(dto.StackIndex);
            writer.Write(dto.IsFaceUp);
            writer.Write(dto.IsRemoved);
            writer.Write(dto.MaxHp);
            writer.Write(dto.CurrentHp);
            writer.Write(dto.Attack);
            writer.Write(dto.Defense);
            writer.Write(dto.ContactDamageToPlayer);
            writer.Write(dto.GoldOnRemoved);
            writer.Write(dto.GoldValue);
            WriteList(writer, dto.RuntimeState, WriteRuntimeStateEntry);
        }

        private static CardInstanceSaveDto ReadCardInstance(BinaryReader reader)
        {
            CardInstanceSaveDto dto = new CardInstanceSaveDto
            {
                InstanceId = reader.ReadUInt32(),
                ModelCategory = reader.ReadString(),
                ModelEntry = reader.ReadString(),
                CardType = reader.ReadInt32(),
                Zone = reader.ReadInt32(),
                CoordRow = null,
                CoordCol = null
            };
            if (reader.ReadBoolean())
            {
                dto.CoordRow = reader.ReadInt32();
                dto.CoordCol = reader.ReadInt32();
            }
            dto.StackIndex = reader.ReadInt32();
            dto.IsFaceUp = reader.ReadBoolean();
            dto.IsRemoved = reader.ReadBoolean();
            dto.MaxHp = reader.ReadInt32();
            dto.CurrentHp = reader.ReadInt32();
            dto.Attack = reader.ReadInt32();
            dto.Defense = reader.ReadInt32();
            dto.ContactDamageToPlayer = reader.ReadInt32();
            dto.GoldOnRemoved = reader.ReadInt32();
            dto.GoldValue = reader.ReadInt32();
            dto.RuntimeState = ReadList(reader, ReadRuntimeStateEntry);
            return dto;
        }

        private static void WriteRuntimeStateEntry(BinaryWriter writer, RuntimeStateEntry dto)
        {
            writer.Write(dto.Key ?? string.Empty);
            writer.Write(dto.Value);
        }

        private static RuntimeStateEntry ReadRuntimeStateEntry(BinaryReader reader)
        {
            return new RuntimeStateEntry
            {
                Key = reader.ReadString(),
                Value = reader.ReadInt32()
            };
        }

        private static void WritePlayerInventory(BinaryWriter writer, PlayerInventorySaveDto dto)
        {
            WriteList(writer, dto.ItemInstanceIds, (w, id) => w.Write(id));
        }

        private static PlayerInventorySaveDto ReadPlayerInventory(BinaryReader reader)
        {
            return new PlayerInventorySaveDto
            {
                ItemInstanceIds = ReadList(reader, r => r.ReadUInt32())
            };
        }

        private static void WriteDungeonDeck(BinaryWriter writer, DungeonDeckSaveDto dto)
        {
            WriteList(writer, dto.CardInstanceIds, (w, id) => w.Write(id));
        }

        private static DungeonDeckSaveDto ReadDungeonDeck(BinaryReader reader)
        {
            return new DungeonDeckSaveDto
            {
                CardInstanceIds = ReadList(reader, r => r.ReadUInt32())
            };
        }

        private static void WriteChoiceSession(BinaryWriter writer, ChoiceSessionSaveDto dto)
        {
            writer.Write(dto.SessionId ?? string.Empty);
            writer.Write(dto.OptionCount);
            writer.Write(dto.ChoiceKind ?? string.Empty);
            writer.Write(dto.IsResolved);
            writer.Write(dto.SelectedOptionIndex);
        }

        private static ChoiceSessionSaveDto ReadChoiceSession(BinaryReader reader)
        {
            return new ChoiceSessionSaveDto
            {
                SessionId = reader.ReadString(),
                OptionCount = reader.ReadInt32(),
                ChoiceKind = reader.ReadString(),
                IsResolved = reader.ReadBoolean(),
                SelectedOptionIndex = reader.ReadInt32()
            };
        }

        private static void WriteRelicInventory(BinaryWriter writer, RelicInventorySaveDto dto)
        {
            WriteList(writer, dto.PassiveRelics, WriteActId);
            WriteNullable(writer, dto.ActiveRelic, WriteActId);
            writer.Write(dto.ActiveRelicMaxUses);
            writer.Write(dto.ActiveRelicUsesRemaining);
        }

        private static RelicInventorySaveDto ReadRelicInventory(BinaryReader reader)
        {
            return new RelicInventorySaveDto
            {
                PassiveRelics = ReadList(reader, ReadActId),
                ActiveRelic = ReadNullable(reader, ReadActId),
                ActiveRelicMaxUses = reader.ReadInt32(),
                ActiveRelicUsesRemaining = reader.ReadInt32()
            };
        }
    }

    public static class RunSaveSerializer
    {
        public static RunSaveDto Capture(Runs.RunState run, Domain.DomainActionContext domain = null)
        {
            RunSaveDto dto = new RunSaveDto
            {
                SaveVersion = 4,
                Seed = run.Seed,
                CurrentActIndex = run.CurrentActIndex,
                IsGameOver = run.IsGameOver,
                RngState = new RngStateDto { Value = run.Rng.CaptureState().Value },
                CurrentMapCoord = run.CurrentMapCoord.HasValue ? new MapCoordSaveDto { Column = run.CurrentMapCoord.Value.Column, Row = run.CurrentMapCoord.Value.Row } : null,
                CurrentRoom = run.CurrentRoom != null ? CaptureRoom(run.CurrentRoom) : null,
                Map = run.Map != null ? CaptureMap(run.Map) : null
            };

            if (domain != null)
            {
                dto.RoomDomainState = DomainSaveAdapter.Capture(domain);
            }

            for (int i = 0; i < run.Players.Count; i++)
            {
                dto.Players.Add(CapturePlayer(run.Players[i]));
            }

            for (int i = 0; i < run.Acts.Count; i++)
            {
                dto.ActIds.Add(new ActIdSaveDto { Category = run.Acts[i].Id.Category, Entry = run.Acts[i].Id.Entry });
            }

            return dto;
        }

        public static Runs.RunState Restore(RunSaveDto dto, out RoomDomainStateSaveDto roomDomainState)
        {
            roomDomainState = dto?.RoomDomainState;
            if (dto == null)
            {
                return null;
            }

            List<Player> players = new List<Player>(dto.Players.Count);
            for (int i = 0; i < dto.Players.Count; i++)
            {
                players.Add(RestorePlayer(dto.Players[i]));
            }

            List<ActModel> acts = new List<ActModel>();
            if (dto.ActIds != null && dto.ActIds.Count > 0)
            {
                for (int i = 0; i < dto.ActIds.Count; i++)
                {
                    acts.Add(ModelDb.Get<ActModel>(new ModelId(dto.ActIds[i].Category, dto.ActIds[i].Entry)));
                }
            }

            Runs.RunState run = new Runs.RunState(dto.Seed, new DeterministicRng(new RngState(dto.RngState != null ? dto.RngState.Value : (uint)dto.Seed)), players, acts)
            {
                CurrentActIndex = dto.CurrentActIndex,
                CurrentMapCoord = dto.CurrentMapCoord != null ? new MapCoord(dto.CurrentMapCoord.Column, dto.CurrentMapCoord.Row) : null,
                IsGameOver = dto.IsGameOver
            };

            if (dto.Map != null)
            {
                run.Map = RestoreMap(dto.Map);
            }

            if (run.CurrentMapCoord.HasValue && run.Map != null && dto.CurrentRoom != null)
            {
                MapPoint point = run.Map.GetPoint(run.CurrentMapCoord.Value);
                RoomFactory roomFactory = new RoomFactory();
                run.CurrentRoom = roomFactory.CreateRoomForMapPoint(run, point);
                RestoreRoomState(run.CurrentRoom, dto.CurrentRoom);
            }

            return run;
        }

        public static Runs.RunState Restore(RunSaveDto dto)
        {
            return Restore(dto, out _);
        }

        private static PlayerSaveDto CapturePlayer(Player player)
        {
            return new PlayerSaveDto
            {
                CharacterCategory = player.Character.Id.Category,
                CharacterEntry = player.Character.Id.Entry,
                CurrentHp = player.Creature.CurrentHp,
                MaxHp = player.Creature.MaxHp,
                Gold = player.Gold
            };
        }

        private static Player RestorePlayer(PlayerSaveDto dto)
        {
            CharacterModel character = ModelDb.Get<CharacterModel>(new ModelId(dto.CharacterCategory, dto.CharacterEntry));
            Player player = new Player(character);
            player.Creature.SetMaxHp(dto.MaxHp);
            player.Creature.SetCurrentHp(dto.CurrentHp);
            player.SetGold(dto.Gold);
            return player;
        }

        private static MapSaveDto CaptureMap(ActMap map)
        {
            MapSaveDto dto = new MapSaveDto
            {
                ColumnCount = map.ColumnCount,
                RowCount = map.RowCount,
                Start = new MapCoordSaveDto { Column = map.StartingMapPoint.Coord.Column, Row = map.StartingMapPoint.Coord.Row },
                Boss = new MapCoordSaveDto { Column = map.BossMapPoint.Coord.Column, Row = map.BossMapPoint.Coord.Row }
            };

            foreach (MapPoint point in map.Points)
            {
                MapPointSaveDto pointDto = new MapPointSaveDto
                {
                    Column = point.Coord.Column,
                    Row = point.Coord.Row,
                    PointType = point.PointType,
                    IsVisited = point.IsVisited,
                    IsCompleted = point.IsCompleted
                };

                foreach (MapPoint child in point.Children)
                {
                    pointDto.Children.Add(new MapCoordSaveDto { Column = child.Coord.Column, Row = child.Coord.Row });
                }

                dto.Points.Add(pointDto);
            }

            return dto;
        }

        private static ActMap RestoreMap(MapSaveDto dto)
        {
            ActMap map = new ActMap(dto.ColumnCount, dto.RowCount);
            Dictionary<MapCoord, MapPoint> points = new Dictionary<MapCoord, MapPoint>(dto.Points.Count);
            for (int i = 0; i < dto.Points.Count; i++)
            {
                MapPointSaveDto pointDto = dto.Points[i];
                MapCoord coord = new MapCoord(pointDto.Column, pointDto.Row);
                MapPoint point = new MapPoint(coord, pointDto.PointType)
                {
                    IsVisited = pointDto.IsVisited,
                    IsCompleted = pointDto.IsCompleted
                };
                points.Add(coord, point);
                map.AddPoint(point);
            }

            for (int i = 0; i < dto.Points.Count; i++)
            {
                MapPointSaveDto pointDto = dto.Points[i];
                MapPoint point = points[new MapCoord(pointDto.Column, pointDto.Row)];
                for (int childIndex = 0; childIndex < pointDto.Children.Count; childIndex++)
                {
                    MapCoordSaveDto childCoord = pointDto.Children[childIndex];
                    point.AddChild(points[new MapCoord(childCoord.Column, childCoord.Row)]);
                }
            }

            map.SetStartingPoint(points[new MapCoord(dto.Start.Column, dto.Start.Row)]);
            map.SetBossPoint(points[new MapCoord(dto.Boss.Column, dto.Boss.Row)]);
            return map;
        }

        private static RoomSaveDto CaptureRoom(AbstractRoom room)
        {
            RoomSaveDto dto = new RoomSaveDto
            {
                RoomType = room.RoomType,
                IsCompleted = room.IsCompleted
            };

            for (int i = 0; i < room.Rewards.Count; i++)
            {
                dto.Rewards.Add(CaptureReward(room.Rewards[i]));
            }

            return dto;
        }

        private static void RestoreRoomState(AbstractRoom room, RoomSaveDto dto)
        {
            room.SetCompleted(dto.IsCompleted);
            List<Rewards.Reward> rewards = new List<Rewards.Reward>(dto.Rewards.Count);
            for (int i = 0; i < dto.Rewards.Count; i++)
            {
                rewards.Add(RestoreReward(dto.Rewards[i]));
            }

            room.SetRewards(rewards);
        }

        private static RewardSaveDto CaptureReward(Rewards.Reward reward)
        {
            RewardSaveDto dto = new RewardSaveDto
            {
                RewardType = reward.Type,
                IsResolved = reward.IsResolved
            };

            if (reward is GoldReward gold)
            {
                dto.GoldAmount = gold.Amount;
            }
            else if (reward is ChoiceReward choice)
            {
                dto.SelectedIndex = choice.SelectedIndex;
                dto.WasSkipped = choice.WasSkipped;
                for (int i = 0; i < choice.Choices.Count; i++)
                {
                    dto.ChoiceLabels.Add(choice.Choices[i]);
                }
            }

            return dto;
        }

        private static Rewards.Reward RestoreReward(RewardSaveDto dto)
        {
            Rewards.Reward reward;
            if (dto.RewardType == Rewards.RewardType.Gold)
            {
                reward = new GoldReward(dto.GoldAmount);
            }
            else
            {
                ChoiceReward choiceReward = new ChoiceReward(dto.ChoiceLabels);
                if (dto.WasSkipped)
                {
                    choiceReward.Skip();
                }
                else if (dto.SelectedIndex >= 0)
                {
                    choiceReward.Select(dto.SelectedIndex);
                }

                reward = choiceReward;
            }

            reward.SetResolvedState(dto.IsResolved);
            return reward;
        }

    }
}
