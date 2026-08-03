from __future__ import annotations

import json
from pathlib import Path

from pypdf import PdfReader


FILES = [
    Path(r"E:\MedSync\MedicalRecognition\docs\参考资料\云南省基本医疗保险医疗服务项目支付目录.pdf"),
    Path(r"E:\MedSync\MedicalRecognition\docs\参考资料\四川省医疗服务价格项目汇编（2022版）.pdf"),
    Path(r"E:\MedSync\MedicalRecognition\docs\参考资料\附件：云南省医疗服务价格项目汇编（2024版）.pdf"),
    Path(r"E:\MedSync\MedicalRecognition\docs\参考资料\辽宁省公立医疗机构医疗服务项目最高限价.pdf"),
]

TERMS = [
    "葡萄糖测定",
    "乙型肝炎表面抗原",
    "CT平扫",
    "钾测定",
    "一类价",
    "最高限价",
    "支付类别",
    "市场调节价",
    "自主制定",
    "市定",
]


for path in FILES:
    reader = PdfReader(path)
    pages = [(page.extract_text() or "").replace("\x00", "") for page in reader.pages]
    matches: dict[str, list[int]] = {}
    for term in TERMS:
        matches[term] = [index + 1 for index, text in enumerate(pages) if term in text][:10]

    print("===CATALOG===")
    print(json.dumps({"file": path.name, "pages": len(pages), "matches": matches}, ensure_ascii=False))

    target_pages = sorted({page for found in matches.values() for page in found[:2]})
    for page_number in target_pages:
        text = pages[page_number - 1]
        relevant_lines = []
        lines = text.splitlines()
        for index, line in enumerate(lines):
            if any(term in line for term in TERMS):
                relevant_lines.extend(lines[max(0, index - 2) : min(len(lines), index + 4)])
        print(f"---PAGE {page_number}---")
        print("\n".join(dict.fromkeys(relevant_lines))[:8000])
