import { ComicInterface } from '../models/comic.interface';

export class ComicsApiMock {
  private baseData: ComicInterface[] =[];

  constructor() {
    this.genMockData();
  }

  private genMockData(): void {
    const datas = [...Array(100).keys()].map(m => {
      const now = new Date();
      const data: ComicInterface = {
        author: `${m}さん`,
        authorKana: ``,
        isbn: `${m}${m}${m}${m}${m}`,
        publisherName: `${m}出版社`,
        salesDate: new Date(now.setDate(now.getDate() + m)),
        scheduleStatus: 3,
        seriesName: ``,
        seriesNameKana: ``,
        title: `${m}なタイトル`,
        titleKana: ``
      };
      return data;
    });
    this.baseData = datas;
  }

  fetchMockDatas(url: string): ComicInterface[] {
    return this.baseData;
  }
}