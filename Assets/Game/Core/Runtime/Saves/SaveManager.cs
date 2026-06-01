using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Core.Cards;
using Game.Core.Entities;
using Game.Core.Map;
using Game.Core.Models;
using Game.Core.Random;
using Game.Core.Rooms;
using Game.Core.Rewards;

namespace Game.Core.Saves
{
    public sealed class SaveManager
    {
        private static readonly string SaveFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TheHollowSketchbook",
            "prototype-current-run.dat");

        private static RunSaveDto _cachedRun;

        public void SaveCurrentRun(Runs.RunState run)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            _cachedRun = RunSaveSerializer.Capture(run);
            PersistToDisk(_cachedRun);
        }

        public Runs.RunState TryLoadCurrentRun()
        {
            if (_cachedRun == null)
            {
                _cachedRun = LoadFromDisk();
            }

            return _cachedRun == null ? null : RunSaveSerializer.Restore(_cachedRun);
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
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
            }
        }

        private static void PersistToDisk(RunSaveDto dto)
        {
            string directory = Path.GetDirectoryName(SaveFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using FileStream stream = new FileStream(SaveFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8);
            RunSaveBinarySerializer.Write(writer, dto);
        }

        private static RunSaveDto LoadFromDisk()
        {
            if (!File.Exists(SaveFilePath))
            {
                return null;
            }

            using FileStream stream = new FileStream(SaveFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
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
            WriteList(writer, dto.Players, WritePlayer);
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

            dto.Players = ReadList(reader, ReadPlayer);
            return dto;
        }

        private static void WritePlayer(BinaryWriter writer, PlayerSaveDto dto)
        {
            writer.Write(dto.CharacterCategory ?? string.Empty);
            writer.Write(dto.CharacterEntry ?? string.Empty);
            writer.Write(dto.CurrentHp);
            writer.Write(dto.MaxHp);
            writer.Write(dto.Gold);
            writer.Write(dto.MaxEnergy);
            WriteList(writer, dto.Deck, WriteCard);
        }

        private static PlayerSaveDto ReadPlayer(BinaryReader reader)
        {
            return new PlayerSaveDto
            {
                CharacterCategory = reader.ReadString(),
                CharacterEntry = reader.ReadString(),
                CurrentHp = reader.ReadInt32(),
                MaxHp = reader.ReadInt32(),
                Gold = reader.ReadInt32(),
                MaxEnergy = reader.ReadInt32(),
                Deck = ReadList(reader, ReadCard)
            };
        }

        private static void WriteCard(BinaryWriter writer, CardSaveDto dto)
        {
            writer.Write(dto.ModelCategory ?? string.Empty);
            writer.Write(dto.ModelEntry ?? string.Empty);
            writer.Write(dto.UpgradeLevel);
            writer.Write(dto.ExhaustOnNextPlay);
            writer.Write(dto.ExtraState.Count);
            foreach (KeyValuePair<string, string> pair in dto.ExtraState)
            {
                writer.Write(pair.Key ?? string.Empty);
                writer.Write(pair.Value ?? string.Empty);
            }
        }

        private static CardSaveDto ReadCard(BinaryReader reader)
        {
            CardSaveDto dto = new CardSaveDto
            {
                ModelCategory = reader.ReadString(),
                ModelEntry = reader.ReadString(),
                UpgradeLevel = reader.ReadInt32(),
                ExhaustOnNextPlay = reader.ReadBoolean(),
                ExtraState = new Dictionary<string, string>()
            };

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                dto.ExtraState.Add(reader.ReadString(), reader.ReadString());
            }

            return dto;
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
            writer.Write(dto.EncounterCategory ?? string.Empty);
            writer.Write(dto.EncounterEntry ?? string.Empty);
            WriteList(writer, dto.Rewards, WriteReward);
        }

        private static RoomSaveDto ReadRoom(BinaryReader reader)
        {
            return new RoomSaveDto
            {
                RoomType = (RoomType)reader.ReadInt32(),
                IsCompleted = reader.ReadBoolean(),
                EncounterCategory = reader.ReadString(),
                EncounterEntry = reader.ReadString(),
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
            WriteList(writer, dto.CardChoices, WriteCard);
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
                CardChoices = ReadList(reader, ReadCard)
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
    }

    public static class RunSaveSerializer
    {
        public static RunSaveDto Capture(Runs.RunState run)
        {
            RunSaveDto dto = new RunSaveDto
            {
                SaveVersion = 1,
                Seed = run.Seed,
                CurrentActIndex = run.CurrentActIndex,
                IsGameOver = run.IsGameOver,
                RngState = new RngStateDto { Value = run.Rng.CaptureState().Value },
                CurrentMapCoord = run.CurrentMapCoord.HasValue ? new MapCoordSaveDto { Column = run.CurrentMapCoord.Value.Column, Row = run.CurrentMapCoord.Value.Row } : null,
                CurrentRoom = run.CurrentRoom != null ? CaptureRoom(run.CurrentRoom) : null,
                Map = run.Map != null ? CaptureMap(run.Map) : null
            };

            for (int i = 0; i < run.Players.Count; i++)
            {
                dto.Players.Add(CapturePlayer(run.Players[i]));
            }

            return dto;
        }

        public static Runs.RunState Restore(RunSaveDto dto)
        {
            if (dto == null)
            {
                return null;
            }

            List<Player> players = new List<Player>(dto.Players.Count);
            for (int i = 0; i < dto.Players.Count; i++)
            {
                players.Add(RestorePlayer(dto.Players[i]));
            }

            List<ActModel> acts = new List<ActModel>
            {
                ModelDb.Get<ActModel>(new ModelId("Act", "PrototypeAct"))
            };

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
                if ((dto.CurrentRoom.RoomType == RoomType.Combat || dto.CurrentRoom.RoomType == RoomType.Boss)
                    && !string.IsNullOrEmpty(dto.CurrentRoom.EncounterCategory)
                    && !string.IsNullOrEmpty(dto.CurrentRoom.EncounterEntry))
                {
                    EncounterModel encounter = ModelDb.Get<EncounterModel>(new ModelId(dto.CurrentRoom.EncounterCategory, dto.CurrentRoom.EncounterEntry));
                    bool isElite = point.PointType == MapPointType.Elite;
                    bool isBoss = dto.CurrentRoom.RoomType == RoomType.Boss;
                    run.CurrentRoom = roomFactory.CreateCombatRoom(point, encounter, isElite, isBoss);
                }
                else
                {
                    run.CurrentRoom = roomFactory.CreateRoomForMapPoint(run, point);
                }

                RestoreRoomState(run.CurrentRoom, dto.CurrentRoom);
            }

            return run;
        }

        private static PlayerSaveDto CapturePlayer(Player player)
        {
            PlayerSaveDto dto = new PlayerSaveDto
            {
                CharacterCategory = player.Character.Id.Category,
                CharacterEntry = player.Character.Id.Entry,
                CurrentHp = player.Creature.CurrentHp,
                MaxHp = player.Creature.MaxHp,
                Gold = player.Gold,
                MaxEnergy = player.MaxEnergy
            };

            IReadOnlyList<CardModel> deck = player.Deck.Cards;
            for (int i = 0; i < deck.Count; i++)
            {
                dto.Deck.Add(CaptureCard(deck[i]));
            }

            return dto;
        }

        private static Player RestorePlayer(PlayerSaveDto dto)
        {
            CharacterModel character = ModelDb.Get<CharacterModel>(new ModelId(dto.CharacterCategory, dto.CharacterEntry));
            Player player = new Player(character);
            player.Deck.Clear();
            for (int i = 0; i < dto.Deck.Count; i++)
            {
                player.AddCardToDeck(RestoreCard(dto.Deck[i]));
            }

            player.Creature.SetMaxHp(dto.MaxHp);
            player.Creature.SetCurrentHp(dto.CurrentHp);
            player.MaxEnergy = dto.MaxEnergy;
            player.SetGold(dto.Gold);
            return player;
        }

        private static CardSaveDto CaptureCard(CardModel card)
        {
            return new CardSaveDto
            {
                ModelCategory = card.Id.Category,
                ModelEntry = card.Id.Entry,
                UpgradeLevel = card.UpgradeLevel
            };
        }

        private static CardModel RestoreCard(CardSaveDto dto)
        {
            CardModel card = ModelDb.CreateMutable<CardModel>(new ModelId(dto.ModelCategory, dto.ModelEntry));
            for (int i = 0; i < dto.UpgradeLevel; i++)
            {
                card.Upgrade();
            }

            return card;
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

            if (room is CombatRoom combatRoom)
            {
                dto.EncounterCategory = combatRoom.Encounter.Id.Category;
                dto.EncounterEntry = combatRoom.Encounter.Id.Entry;
            }

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
            else if (reward is CardRewardChoice cardChoice)
            {
                dto.SelectedIndex = cardChoice.SelectedIndex;
                dto.WasSkipped = cardChoice.WasSkipped;
                for (int i = 0; i < cardChoice.Choices.Count; i++)
                {
                    dto.CardChoices.Add(CaptureCard(cardChoice.Choices[i]));
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
                List<CardModel> choices = new List<CardModel>(dto.CardChoices.Count);
                for (int i = 0; i < dto.CardChoices.Count; i++)
                {
                    choices.Add(RestoreCard(dto.CardChoices[i]));
                }

                CardRewardChoice cardReward = new CardRewardChoice(choices);
                if (dto.WasSkipped)
                {
                    cardReward.Skip();
                }
                else if (dto.SelectedIndex >= 0)
                {
                    cardReward.Select(dto.SelectedIndex);
                }

                reward = cardReward;
            }

            reward.SetResolvedState(dto.IsResolved);
            return reward;
        }
    }
}
