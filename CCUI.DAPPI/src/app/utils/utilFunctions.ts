import { EnumKvp } from "../models/content.model";

export function parseEnum<T>(enumObj: T): EnumKvp[] {
    const kvp: EnumKvp[] = [];
    for (let key in enumObj) {
        if (!isNaN(Number(key))) {
            let enumKey: number = Number(key);
            let enumValue: string = enumObj[key] as string;
            kvp.push({ value: enumKey, label: enumValue })
        }
    }
    return kvp;
}

export function parseEnumFromNumberArray<T extends Record<number,string>>(enumObj:T, keys:number[]):EnumKvp[]{
    const kvp: EnumKvp[] = [];
    for (let key in keys) {
        let enumKey: number = Number(key);
        let enumValue: string = enumObj[key] as string;
        kvp.push({ value: enumKey, label: enumValue })
    }
    return kvp;
}