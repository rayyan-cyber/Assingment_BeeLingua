using Assingment_BeeLingua.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using Nexus.Base.CosmosDBRepository;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Assingment_BeeLingua.BLL.Services
{
    public class LessonService
    {
        private readonly IDocumentDBRepository<Lesson> _repository;
        public LessonService(IDocumentDBRepository<Lesson> repository)
        {
            if (this._repository == null)
            {
                this._repository = repository;
            }
        }

        public async Task<Lesson> GetLessonById(string id, Dictionary<string, string> pk)
        {
            return await _repository.GetByIdAsync(id, pk);
        }

        public async Task<IEnumerable<Lesson>> GetLessonAll()
        {
            var data = await _repository.GetAsync(predicate: null);
            return data.Items;
        }

        public async Task<Lesson> CreateLesson(Lesson dataToBeInserted)
        {
            return await _repository.CreateAsync(dataToBeInserted);
        }

        public async Task<Lesson> CreateLessonEdited(string id)
        {
            var lesson = await _repository.GetByIdAsync(id);
            //lesson.Id = null;
            lesson.Description += "-Edited";

            var lessonNew = await _repository.CreateAsync(lesson);
            return lessonNew;
        }

        public async Task<Lesson> UpdateLesson(string id, Lesson dataToBeUpdated)
        {
            return await _repository.UpdateAsync(id, dataToBeUpdated);
        }

        public async Task<IActionResult> DeleteLesson(string id, Dictionary<string, string> pk)
        {
            await _repository.DeleteAsync(id, pk);
            var result = new OkObjectResult("Deleted");
            return result;
        }
    }
}
