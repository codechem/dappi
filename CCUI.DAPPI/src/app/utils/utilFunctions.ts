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

export function extractErrorMessage(error: any, fallback = 'Unknown error'): string {

    const payload = error?.error;

    if (typeof payload === 'string' && payload.trim()) return payload;
    if (typeof payload?.message === 'string' && payload.message.trim()) return payload.message;
    if (typeof payload?.title === 'string' && payload.title.trim()) return payload.title;
    if (typeof error?.message === 'string' && error.message.trim()) return error.message;

    return payload ? JSON.stringify(payload) : fallback;
}