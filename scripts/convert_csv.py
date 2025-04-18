import csv
import json

def remove_nashi(xs):
    return [x for x in xs if x != 'なし' and x != '']

def parse_ox(ox):
    return ox == 'O'

class StationData:
    def __init__(self, station_id, name, is_station, is_passenger_station):
        self.Id = station_id
        self.Name = name
        self.IsStation = parse_ox(is_station)
        self.IsPassengerStation = parse_ox(is_passenger_station)

class TrackCircuitData:
    def __init__(self, name, NextSignalNamesUp, NextSignalNamesDown, protectionZone):
        self.Name = name
        self.Last = ''
        self.On = False
        self.NextSignalNamesUp = remove_nashi(NextSignalNamesUp)
        self.NextSignalNamesDown = remove_nashi(NextSignalNamesDown)
        self.ProtectionZone = int(protectionZone) if protectionZone != '' else None

class SignalData:
    def __init__(self, name, type_name, next_signal_names, route_names):
        self.Name = name
        self.phase = 1
        self.TypeName = type_name
        self.NextSignalNames = remove_nashi(next_signal_names)
        self.RouteNames = remove_nashi(route_names)

class SignalTypeData:
    def __init__(self, name, r_indication, yy_indication, y_indication, yg_indication, g_indication):
        self.Name = name
        self.RIndication = r_indication
        self.YYIndication = yy_indication
        self.YIndication = y_indication
        self.YGIndication = yg_indication
        self.GIndication = g_indication

class ThrowOutControlData:
    def __init__(self, source_route_name, target_route_name, lever_condition_name):
        self.SourceRouteName = source_route_name
        self.TargetRouteName = target_route_name
        self.LeverConditionName = lever_condition_name

class DBBasejson:
    def __init__(self):
        self.stationList = []
        self.trackCircuitList = []
        self.signalDataList = []
        self.signalTypeList = []
        self.throwOutControlList = []

def read_csv(file_path, data_class, *args):
    data_list = []
    with open(file_path, newline='', encoding='utf-8') as csvfile:
        reader = csv.reader(csvfile)
        next(reader)  # Skip header
        for row in reader:
            init_args = []
            for arg in args:
                if isinstance(arg, list):
                    init_args.append([row[i] for i in arg])
                else:
                    init_args.append(row[arg])
            data_list.append(data_class(*init_args))
    return data_list

def main():
    db = DBBasejson()
    db.stationList = read_csv(
        '../Traincrew_MultiATS_Server/Data/駅・停車場.csv',
        StationData, 0, 1, 2, 3
    )
    db.trackCircuitList = read_csv(
        '../Traincrew_MultiATS_Server/Data/軌道回路に対する計算するべき信号機リスト.csv',
        TrackCircuitData, 0, [1, 2, 3, 4, 5], [7, 8, 9, 10, 11], 13
    )
    db.signalDataList = [e for e in read_csv(
        '../Traincrew_MultiATS_Server/Data/信号リスト.csv',
        SignalData, 0, 1, [2, 3, 4, 5, 6], [8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18]
    ) if e.Name != 'なし']
    db.signalTypeList = read_csv(
        '../Traincrew_MultiATS_Server/Data/信号何灯式リスト.csv',
        SignalTypeData, 0, 1, 2, 3, 4, 5
    )
    db.throwOutControlList = read_csv(
        '../Traincrew_MultiATS_Server/Data/総括制御ペア一覧.csv',
        ThrowOutControlData, 0, 1, 2
    )

    with open('../Traincrew_MultiATS_Server/Data/DBBase.json', 'w', encoding='utf-8') as jsonfile:
        json.dump(db.__dict__, jsonfile, ensure_ascii=False, indent=4, default=lambda o: o.__dict__)

if __name__ == "__main__":
    main()
