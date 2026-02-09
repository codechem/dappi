export enum MediaUploadStatus {
  Unknown = 0,
  Pending = 1,
  Completed = 2,
  Failed = 3,
}

export interface MediaInfo {
  Id: string;
  Url: string;
  OriginalFileName: string;
  FileSize: number;
  UploadDate: string;
  Status: MediaUploadStatus | null
}
