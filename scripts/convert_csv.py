import csv
import json

def remove_nashi(xs):
    return [x for x in xs if x != 'なし' and x != '']

class TrackCircuitData:
    def __init__(self, name, NextSignalNamesUp, NextSignalNamesDown):
        self.Name = name
        self.Last = ''
        self.On = False
        self.NextSignalNamesUp = remove_nashi(NextSignalNamesUp)
        self.NextSignalNamesDown = remove_nashi(NextSignalNamesDown)

class SignalData:
    def __init__(self, name, type_name, next_signal_names):
        self.Name = name
        self.phase = 1
        self.TypeName = type_name
        self.NextSignalNames = remove_nashi(next_signal_names)

class SignalTypeData:
    def __init__(self, name, r_indication, yy_indication, y_indication, yg_indication, g_indication):
        self.Name = name
        self.RIndication = r_indication
        self.YYIndication = yy_indication
        self.YIndication = y_indication
        self.YGIndication = yg_indication
        self.GIndication = g_indication

class DBBasejson:
    def __init__(self):
        self.trackCircuitList = []
        self.signalDataList = []
        self.signalTypeList = []

def read_csv(file_path, data_class, *args):
    data_list = []
    with open(file_path, newline='', encoding='utf-8') as csvfile:
        reader = csv.reader(csvfile)
        next(reader)  # Skip header
        for row in reader:
            if row[0] == 'なし':
                continue
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
    db.trackCircuitList = read_csv(
        '../Traincrew_MultiATS_Server/Data/軌道回路に対する計算するべき信号機リスト.csv',
        TrackCircuitData, 0, [1, 2, 3, 4, 5], [7, 8, 9, 10, 11]
    )
    db.signalDataList = read_csv(
        '../Traincrew_MultiATS_Server/Data/信号リスト.csv',
        SignalData, 0, 1, [2, 3, 4, 5, 6]
    )
    db.signalTypeList = read_csv(
        '../Traincrew_MultiATS_Server/Data/信号何灯式リスト.csv',
        SignalTypeData, 0, 1, 2, 3, 4, 5
    )

    with open('../Traincrew_MultiATS_Server/Data/DBBase.json', 'w', encoding='utf-8') as jsonfile:
        json.dump(db.__dict__, jsonfile, ensure_ascii=False, indent=4, default=lambda o: o.__dict__)

if __name__ == "__main__":
    main()
