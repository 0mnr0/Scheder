
from joblib import load
from skl2onnx import to_onnx
from skl2onnx.common.data_types import StringTensorType

MODEL_PATH = "dataset.joblib"
ONNX_PATH = "../dataset.onnx"

def main():
    clf = load(MODEL_PATH)

    initial_type = [("input", StringTensorType([None, 1]))]

    onx = to_onnx(
        clf,
        initial_types=initial_type,
        options={"zipmap": False},
    )

    with open(ONNX_PATH, "wb") as f:
        f.write(onx.SerializeToString())

    print(f"Готово: {ONNX_PATH}")

    for inp in onx.graph.input:
        print("INPUT :", inp.name)
    for out in onx.graph.output:
        print("OUTPUT:", out.name)

if __name__ == "__main__":
    main()