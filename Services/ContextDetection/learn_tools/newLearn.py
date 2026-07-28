import hashlib
import json
import os
import re
from dataclasses import dataclass, field
from typing import List, Tuple

from collections import Counter
import numpy as np
from joblib import dump, load
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.linear_model import LogisticRegression
from sklearn.pipeline import Pipeline, FeatureUnion
from sklearn.calibration import CalibratedClassifierCV
from sklearn.metrics import (
    classification_report,
    precision_recall_curve,
    f1_score,
)
from sklearn.model_selection import StratifiedKFold, cross_val_predict

MODEL_PATH = "dataset.joblib"
META_PATH = "dataset.meta.json"
FALLBACK_THRESHOLD = 0.625


def normalize_text(s: str) -> str:
    s = s.lower()
    s = s.replace("ё", "е")
    s = re.sub(r"\bзатр[аи]\b", "завтра", s)
    s = re.sub(r"\bзавтро\b", "завтра", s)
    s = re.sub(r"[^\w\s]", " ", s, flags=re.UNICODE)
    s = re.sub(r"\s+", " ", s).strip()
    return s


NEG_FALSE_PATTERNS = [
    r"не\s+буд\w+.*пар",
    r"пар[ауеы]?\s+не\s+буд\w+",
    r"нет\s+пар",
    r"пар\s+нет",
    r"не\s+хочу.*пар",
    r"не\s+буду.*пар",
    r"сегодня\s+нет\s+пар",
    r"завтра\s+нет\s+пар",
]


def has_strong_negation(t: str) -> bool:
    t = normalize_text(t)
    return any(re.search(p, t) for p in NEG_FALSE_PATTERNS)


@dataclass
class Sample:
    text: str
    label: int  # 1 = запрос расписания, 0 = не запрос
    weight: float = 1.0


base_samples: List[Sample] = [
    Sample("пары", 1, weight=1.5),
    Sample("пары завтра", 1, weight=2.0),
    Sample("Какие затра пары?", 1),
    Sample("Расписание завтра", 1),
    Sample("пары завтрв", 1),
    Sample("пары зщвтрв", 1),
    Sample("пары зщвтрв", 1),
    Sample("что у нас завтра по парам?", 1),
    Sample("пары послезавтра", 1, weight=1.5),
    Sample("Есть ли на завтра расписание по парам?", 1),
    Sample("пары после завтра", 1),
    Sample("какие пары на завтра", 1),
    Sample("пары на завтра", 1),
    Sample("Сколько пар у меня завтра?", 1),
    Sample("Расписание на сегодня", 1),
    Sample("Будут ли завтра пары?", 1),
    Sample("Можно узнать расписание на завтра?", 1),
    Sample("У меня завтра будут пара по математике?", 1),
    Sample("Завтра не будет пар?", 1),
    Sample("пары в понедельник", 1),
    Sample("какие пары в понедельник", 1),
    
    Sample("пары понедельник", 1),
    Sample("пары вторник", 1),
    Sample("пары среда", 1),
    Sample("пары четверг", 1),
    Sample("пары пятница", 1),
    Sample("пары суббота", 1),
    Sample("пары воскресенье", 1),
    Sample("пары в понедельник", 1),
    Sample("пары во вторник", 1),
    Sample("пары в среду", 1),
    Sample("пары в четверг", 1),
    Sample("пары в пятницу", 1),
    Sample("пары в субботу", 1),
    Sample("пары в воскресенье", 1),
    Sample("пары в пн", 1),
    Sample("пары во вт", 1),
    Sample("пары в ср", 1),
    Sample("пары в чт", 1),
    Sample("пары в пт", 1),
    Sample("пары в сб", 1),
    Sample("пары в вс", 1),
    
    Sample("пары 10.10", 1),
    Sample("пары на 10.10", 1),
    Sample("пары в 10.10", 1),
    Sample("какие", 1, weight=0.1),
    Sample("пары ну ту неделю", 1),
    Sample("пары на эту неделю", 1),
    Sample("пары на следующую неделю", 1),
    Sample("пары на предыдущую неделю", 1),
    Sample("какие пары были вчера", 1),
    Sample("какие пары послезавтра", 1),
    Sample("А какие завтра пары?", 1),
    Sample("что по парам завтра", 1),
    Sample("пары завтра будут", 1),
    Sample("расписание пары завтра", 1),
    Sample("расписание на завтра", 1),
    Sample("есть пары сегодня?", 1),
    Sample("подскажите пары на", 1),
    Sample("подскажите пары на сегодня", 1),
    Sample("подскажите пары на завтра", 1),
    Sample("скажите какие у нас пары", 1),
    Sample("скажите какие у нас пары сегодня", 1),
    Sample("сколько пар сегодня", 1),
    Sample("какие пары сегодня", 1),
    Sample("какая первая пара завтра", 1),
    Sample("у меня завтра пары?", 1),
    Sample("завтра пары есть?", 1),
    Sample("пары +?", 1),
    Sample("пары -?", 1),
    Sample("пары вчера", 1),
    Sample("покажи пары", 1),
    Sample("покажи пары на", 1),
    Sample("напомните пары", 1),
    Sample("подскажите пары", 1),
    Sample("напомните пары на", 1),
    Sample("какие будут пары", 1, weight=1), 
    Sample("когда будут пары", 1, weight=0.5),
    Sample("вы можете написать пары на завтра", 1),
    Sample("можно спросить пары на", 1),
    Sample("можно спросить пары на завтра", 1),
    Sample("что за пары", 1),
    Sample("что за пары завтра", 1),
    Sample("что за пары сегодня", 1),
    Sample("паоы", 1),
    Sample("парф", 1),
    Sample("парв", 1),
    Sample("пары никогда", 1),
    
    
 
    Sample("почему у нас стоят пары", 0),
    Sample("завтра спляшешь", 0),
    Sample("завтра пойдешь ", 0),
    Sample("го гулять завтра", 0),
    Sample("пойдешь гулять завтра", 0),
    Sample("сегодня идешь и рассказываешь", 0),
    Sample("будешь учиться завтра", 0),
    Sample("пошли бухать завтра", 0),
    Sample("в футбик будешь ", 0),
    Sample("завтра понедельник", 0),
    Sample("завтра вторник", 0),
    Sample("завтра среда", 0),
    Sample("завтра четверг", 0),
    Sample("завтра пятница", 0),
    Sample("завтра суббота", 0),
    Sample("завтра здохну", 0),
    Sample("спать хочу", 0),
    Sample("а ну разошлись", 0),
    Sample("пары почему", 0, weight=1.25),
    
    
    
    Sample("на какой паре зачет", 0),
    Sample("какой парой зачет", 0),
    Sample("сколько пар должно быть", 0),
    Sample("зачёт - парой", 0),
    Sample("зачёт какой парой", 0),
    Sample("будут пары, нет?", 0),
    Sample("а пары то будут?", 0),
    Sample("это че за ужас", 0),
    Sample("это чо за пары", 0),
    Sample("что за", 0),
    Sample("вы можете написать информацию", 0),
    Sample("вы можете написать что случилось", 0),
    Sample("можно спросить какой результат", 0),
    Sample("можно спросить что случилось", 0),
    Sample("что это за пары", 0),
    Sample("что это за пары завтра", 0),
    Sample("пора", 0),
    Sample("пора работать", 0),
    Sample("пора по делам", 0),
    Sample("Сегодня вообще нет пар", 0),
    Sample("что нужно узнать", 0),
    Sample("хотите узнать", 0),
    Sample("Не буду сегодня на парах", 0),
    Sample("он занят, так что пар не будет", 0),
    Sample("пар не будет", 0),
    Sample("почему вас нет на парах", 0),
    Sample("Ну хорошо: вечером приду на пары", 0),
    Sample("Какая пара?", 0),
    Sample("кстати идти не надо", 0),
    Sample("кстати завтра никуда идти не надо", 0),
    Sample("Не хочу завтра на пары", 0),
    Sample("Я завтра приду на пары", 0),
    Sample("а у нас завтра пар не будет чтоли?", 0),
    Sample("я спросил, пар не будет", 0),
    Sample("сказали на пары не идти", 0),
    Sample("у нас нет пар завтра", 0),
    Sample("завтра пар нет", 0),
    Sample("я на пары не приду", 0),
    Sample("я завтра на пары не приду", 0),
    Sample("я на пары не приду завтра", 0),
    Sample("ничего про пары", 0),
    Sample("вечером буду дома", 0),
    Sample("скорее всего пар не будет", 0),
    Sample("пары отменили", 0),
    Sample("сегодня без пар", 0),
    Sample("На пары надо ходить", 0),
    Sample("на улице пар", 0),
    Sample("емааа", 0),
    Sample("ема", 0),
    Sample("ема пары", 0),
    Sample("лох", 0),
    Sample("пидр", 0),
    Sample("пидор", 0),
    Sample("каждой тваре по паре", 0),
    Sample("парапам", 0),
    Sample("почему у нас были пары", 0),
    Sample("почему у нас завтра пары", 0, weight=1.2),
    Sample("почему завтра пары", 0, weight=1.2),
    Sample("почему завтра стоят пары", 0, weight=1.2),
    Sample("почему у нас стоят пары", 0),
    Sample("никто об этом не сказал", 0),
    Sample("молчишь", 0),
    Sample("чешешь", 0),
    Sample("с легким паром", 0),
    Sample("парить есть что", 0),
    Sample("дай испарик", 0),
    Sample("испарик", 0),
    Sample("то есть", 0),
    Sample("пары были", 0),
    Sample("я приду к - паре", 0),
    Sample("я приду к -й паре", 0),
    Sample("Возможно опоздаю на пару", 0),
    Sample("Возможно не успеваю на пары", 0),
    Sample("До конца пары не успеешь", 0),
    Sample("До конца пары успеешь", 0),
    Sample("Пары отсидеть", 0),
    Sample("", 0),
    Sample(" ", 0),
    Sample("каждые день пары", 0),
    Sample("витая пара", 0),
    Sample("Кто придёт на пару", 0),
    Sample("Кто к паре", 0),
    Sample("Кто к какой паре придет", 0),
    Sample("кто придет вовремя", 0),
    Sample("придем на пары вовремя", 0),
    Sample("Почему людей так мало", 0),
    Sample("завтра есть пары", 0),
    Sample("почему", 0, weight=1.3),
    Sample("зачем", 0),
    Sample("куда", 0),
    Sample("пройти", 0),
    Sample("Пары между зачётами", 0),
    Sample("будут пары между зачёт", 0),
    Sample("поработаем на паре", 0),
    Sample("закидывать пары", 0),
    Sample("швыряет парту", 0),
    Sample("парта", 0),
    Sample("ты что делаешь", 0),
    Sample("надо идти на пары", 0),
    Sample("или нет", 0),
    Sample("так надо идти или нет", 0),
    Sample("закидывает пары", 0),
    Sample("Я к x паре", 0),
    Sample("приду на - пару", 0),
    Sample("оцените пары", 0),
    Sample("задание на паре", 0),
    Sample("спим на паре", 0),
    Sample("пара вещей", 0),
    Sample("пара дней", 0),
    Sample("пару дней", 0),
    Sample("пару человек", 0),
    Sample("пару часов", 0),
    Sample("пару часов", 0),
    Sample("пары сделают меня", 0),
    Sample("ебал на пару", 0, weight=2),
    Sample("не показывай пары", 0),
    Sample("s", 0), Sample("ы", 0), Sample("а", 0), Sample(".", 0), Sample("g", 0),
    Sample("п", 0), Sample("bruh", 0), Sample("0", 0), Sample("й", 0), Sample("q", 0),
    Sample("qq", 0), Sample("ч", 0), Sample("\\", 0), Sample("м", 0), Sample("+", 0),
    Sample("-", 0), Sample("л", 0), Sample("ъ", 0), Sample("ь", 0), Sample("щ", 0),
    Sample("х", 0), Sample("шо", 0), Sample("ничо", 0), Sample("чё", 0),
    
    
    
    Sample("парк", 0, weight=1.2), Sample("парта", 0, weight=1.2), Sample("паром", 0, weight=1.2), Sample("парадокс", 0, weight=1.2)
]


def make_xyw(samples: List[Sample]) -> Tuple[List[str], np.ndarray, np.ndarray]:
    X = [normalize_text(s.text) for s in samples]
    y = np.array([s.label for s in samples])
    w = np.array([s.weight for s in samples])
    return X, y, w


def dataset_hash(samples: List[Sample]) -> str:
    payload = json.dumps(
        [[s.text, s.label, s.weight] for s in samples],
        ensure_ascii=False, sort_keys=False,
    )
    return hashlib.md5(payload.encode("utf-8")).hexdigest()



def build_pipeline() -> Pipeline:
    char_vec = TfidfVectorizer(analyzer="char", ngram_range=(3, 5),
                                lowercase=True, max_features=30000, sublinear_tf=True)
    word_vec = TfidfVectorizer(analyzer="word", ngram_range=(1, 2),
                                lowercase=True, max_features=10000, sublinear_tf=True, min_df=1)
    features = FeatureUnion([("char", char_vec), ("word", word_vec)])

    base_clf = LogisticRegression(max_iter=2000, C=0.8, random_state=42)
    calibrated_clf = CalibratedClassifierCV(base_clf, method="sigmoid", cv=3)

    return Pipeline([("features", features), ("clf", calibrated_clf)])

def class_balance(samples: List[Sample]):
    counts = Counter(s.label for s in samples)
    weighted = Counter()
    for s in samples:
        weighted[s.label] += s.weight

    total_count = sum(counts.values())
    total_weight = sum(weighted.values())

    print("== Баланс классов ==")
    for label in sorted(counts):
        print(f"label={label}: "
              f"{counts[label]} примеров ({counts[label]/total_count:.1%}), "
              f"суммарный вес {weighted[label]:.1f} ({weighted[label]/total_weight:.1%})")

def evaluate_cv(X: List[str], y: np.ndarray, w: np.ndarray, n_splits: int = 5):
    """Честная оценка через StratifiedKFold + подбор порога по PR-кривой."""
    skf = StratifiedKFold(n_splits=n_splits, shuffle=True, random_state=42)

    proba = np.zeros(len(y), dtype=float)
    for train_idx, test_idx in skf.split(X, y):
        pipe = build_pipeline()
        X_train = [X[i] for i in train_idx]
        y_train = y[train_idx]
        w_train = w[train_idx]
        pipe.fit(X_train, y_train, clf__sample_weight=w_train)

        X_test = [X[i] for i in test_idx]
        proba[test_idx] = pipe.predict_proba(X_test)[:, 1]

    precisions, recalls, thresholds = precision_recall_curve(y, proba)
    f1s = 2 * precisions * recalls / (precisions + recalls + 1e-12)
    best_idx = int(np.nanargmax(f1s[:-1])) if len(thresholds) else 0
    best_threshold = float(thresholds[best_idx]) if len(thresholds) else FALLBACK_THRESHOLD

    y_pred_at_best = (proba >= best_threshold).astype(int)
    print(f"== Кросс-валидация ({n_splits} фолдов) ==")
    print(classification_report(y, y_pred_at_best, digits=3))
    print(f"Рекомендованный порог (max F1): {best_threshold:.3f}")

    return best_threshold


def train_and_save():
    X, y, w = make_xyw(base_samples)
    best_threshold = evaluate_cv(X, y, w)

    clf = build_pipeline()
    clf.fit(X, y, clf__sample_weight=w)
    dump(clf, MODEL_PATH)

    meta = {
        "dataset_hash": dataset_hash(base_samples),
        "threshold": best_threshold,
    }
    with open(META_PATH, "w", encoding="utf-8") as f:
        json.dump(meta, f, ensure_ascii=False, indent=2)

    print(f"Сохранено: {MODEL_PATH}, {META_PATH}")


def ensure_model():
    need_train = not os.path.exists(MODEL_PATH) or not os.path.exists(META_PATH)

    if not need_train:
        with open(META_PATH, "r", encoding="utf-8") as f:
            meta = json.load(f)
        if meta.get("dataset_hash") != dataset_hash(base_samples):
            need_train = True

    if need_train:
        train_and_save()


def load_threshold() -> float:
    if os.path.exists(META_PATH):
        with open(META_PATH, "r", encoding="utf-8") as f:
            return float(json.load(f)["threshold"])
    return FALLBACK_THRESHOLD


# --------------------------
# Инференс
# --------------------------
class ScheduleClassifier:
    def __init__(self, path: str = MODEL_PATH):
        self.pipeline: Pipeline = load(path)
        self.threshold = load_threshold()

    def predict_proba(self, text: str) -> float:
        t = normalize_text(text)
        proba = self.pipeline.predict_proba([t])[0][1]
        return float(proba)

    def is_schedule_query(self, text: str, threshold: float = None) -> bool:
        if has_strong_negation(text):
            return False
        p = self.predict_proba(text)
        return p >= (threshold if threshold is not None else self.threshold)


ensure_model()
clf = ScheduleClassifier(MODEL_PATH)


def GetCommandWeight(txt) -> tuple[bool, float]:
    return clf.is_schedule_query(txt), clf.predict_proba(txt)

train_and_save()
clf = ScheduleClassifier(MODEL_PATH)

if __name__ == "__main__":
    print('Use "train" to retrain, "exit" to quit\n')

    while True:
        text = input("> ")
        if text == "train":
            train_and_save()
            clf = ScheduleClassifier(MODEL_PATH)
            print("\n")
            continue
        if text == "clear":
            os.system("clear")
            print("\n")
            continue
        if text == "balance":
            class_balance(base_samples)
            print("\n")
            continue
        if text == "exit":
            break
        print(f"-> {clf.is_schedule_query(text)}  (p={clf.predict_proba(text):.3f}, "
              f"порог={clf.threshold:.3f})\n")